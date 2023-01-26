// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper;
using static Friflo.Json.Fliox.JsonKeyType;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    /// <summary>
    /// A struct optimized to store JSON strings representing integers, strings or GUID's<br/>
    /// E.g. <c>"12345", "article" or "550e8400-e29b-11d4-a716-446655440000"</c><br/>
    /// A <see cref="JsonKey"/> can also represents a <c>null</c> value. It can be tested using <see cref="IsNull"/>.<br/>
    /// </summary>
    /// <remarks>
    /// The main goal of optimization is to avoid allocations for the types mentioned above.<br/>
    /// Integers and GUID's are stored inside the struct. Strings with length less than 15 characters are also
    /// stored inside the struct to avoid heap allocations.
    /// </remarks>
    public readonly struct JsonKey
    {
        // TODO could store type in long lng2 to increase length of short strings from 15 to 22 by using unused bytes in enum
        internal    readonly    JsonKeyType type;
        internal    readonly    string      str;
        internal    readonly    long        lng;  // long  |  lower  64 bits for Guid  | lower  8 bytes for UTF-8 string
        [Browse(Never)]
        internal    readonly    long        lng2; //          higher 64 bits for Guid  | higher 7 bytes for UTF-8 string + 1 byte length
        
        public                  JsonKeyType Type        => type;
        internal                Guid        Guid        => GuidUtils.LongLongToGuid(lng, lng2);
        public      override    string      ToString()  => GetString(); 

        public static readonly  JsonKeyComparer         Comparer    = new JsonKeyComparer();
        public static readonly  JsonKeyEqualityComparer Equality    = new JsonKeyEqualityComparer();

        /// <summary>
        /// Calling this constructor should be the last option as it may force a string creation. <br/>
        /// Use alternative constructors if using a specific key type like <see cref="long"/> or <see cref="Guid"/>.
        /// </summary>
        public JsonKey (string value)
        {
            if (value == null) {
                type    = NULL;
                str     = null;
                lng     = 0;
                lng2    = 0;
                return;
            }
            if (long.TryParse(value, out long result)) {
                type    = LONG;
                str     = null;
                lng     = result;
                lng2    = 0;
                return;
            }
            var stringLength = value.Length;
            if (stringLength == Bytes.GuidLength && Guid.TryParse(value, out var guid)) {
                type    = GUID;
                str     = null;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = STRING;
            ShortStringUtils.StringToLongLong(value, out str, out lng, out lng2);
        }
        
        public JsonKey (ref Bytes bytes, ref ValueParser valueParser, in JsonKey oldKey) {
            if (bytes.IsIntegral()) {
                type    = LONG;
                str     = null;
                var error = new Bytes();
                lng     = valueParser.ParseLong(ref bytes, ref error, out bool success);
                if (!success)
                    throw new InvalidOperationException("expect a valid integral type");
                lng2    = 0;
                return;
            }
            var bytesLen = bytes.end - bytes.start;
            if (bytesLen == Bytes.GuidLength && bytes.TryParseGuid(out var guid, out string temp)) { // temp not null in Unity. Otherwise null
                type    = GUID;
                str     = temp;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = STRING;
            if (ShortStringUtils.BytesToLongLong(bytes, out lng, out lng2)) {
                str     = null;
            } else {
                str     = ShortString.GetString(bytes, oldKey.str);
                lng     = 0;
                lng2    = 0;
            }
        }

        public JsonKey (long value) {
            type    = LONG;
            str     = null;
            lng     = value;
            lng2    = 0;
        }
        
        public JsonKey (long? value) {
            type    = value.HasValue ? LONG : NULL;
            str     = null;
            lng     = value ?? 0;
            lng2    = 0;
        }
        
        public JsonKey (in Guid guid) {
            type    = GUID;
            str     = null;
            GuidUtils.GuidToLongLong(guid, out lng, out lng2);
        }
        
        public JsonKey (in Guid? guid) {
            var hasValue= guid.HasValue;
            type        = hasValue ? GUID : NULL;
            str         = null; // hasValue ? guid.ToString() : null;
            if (hasValue) {
                GuidUtils.GuidToLongLong(guid.Value, out lng, out lng2);
                return;
            }
            lng     = 0;
            lng2    = 0;
        }
        
        public bool IsNull() {
            switch (type) {
                case LONG:
                case STRING:
                case GUID:      return false;
                case NULL:      return true;
                default:
                    throw new InvalidOperationException($"invalid JsonKey: {AsString()}");
            }
        }
        
        private const int MaxCharCount = 16; // Encoding.UTF8.GetMaxCharCount(15);
        
        internal static int StringCompare(in JsonKey left, in JsonKey right)
        {
            if (left.type   != STRING) throw new ArgumentException("expect left.type: STRING");
            if (right.type  != STRING) throw new ArgumentException("expect right.type: STRING");
            
            var leftStr     = new ShortString(left);
            var rightStr    = new ShortString(right);
            return ShortString.StringCompare(leftStr, rightStr);
        }
        
        public bool IsEqual(in JsonKey other) {
            if (type != other.type)
                return false;
            
            switch (type) {
                case LONG:      return lng  == other.lng;
                case STRING:
                    if (str == null && other.str == null) {
                        return lng == other.lng && lng2 == other.lng2;
                    }
                    // In case one str field is null and the other is set strings are not equal as one value is a
                    // short string (str == null) with length <= 15 and the other a string instance with length > 15.
                    return str == other.str;
                case GUID:      return lng  == other.lng && lng2 == other.lng2;
                case NULL:      return true;
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }

        public int HashCode() {
            switch (type) {
                case LONG:      return lng. GetHashCode();
                case STRING:    return str?.GetHashCode() ?? lng.GetHashCode() ^ lng2.GetHashCode();
                case GUID:      return lng. GetHashCode() ^ lng2.GetHashCode();
                case NULL:      return 0;
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual() or JsonKey.Equality comparer");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use JsonKey.Equality comparer");
        }
        
        private string GetString() {
            if (type == NULL) {
                return "null";
            }
            return $"'{AsString()}'";
        }

        /// <summary>Calling this method causes string instantiation. To avoid this use its <i>AppendTo</i> methods if possible.</summary> 
        public string AsString() {
            switch (type) {
                case LONG:      return str ?? lng. ToString();
                case STRING:
                    if (str != null) {
                        return str;
                    }
                    Span<char> chars    = stackalloc char[MaxCharCount];
                    var length          = ShortStringUtils.GetChars(lng, lng2, chars);
                    var readOnlySpan    = chars.Slice(0, length);
                    return new string(readOnlySpan);
                case GUID:      return str ?? Guid.ToString();
                case NULL:      return null;
                default:
                    throw new InvalidOperationException($"unexpected type in JsonKey.AsString(). type: {type}");
            }
        }
        
        public long AsLong() {
            if (type == LONG)
                return lng;
            throw new InvalidOperationException($"cannot return JsonKey as long. {AsString()}");
        }
        
        public Guid AsGuid() => Guid;
        
        public Guid? AsGuidNullable() {
            return type == GUID ? Guid : default; 
        }
        
        public void AppendTo(ref Bytes dest, ref ValueFormat valueFormat) {
            switch (type) {
                case LONG:
                    valueFormat.AppendLong(ref dest, lng);
                    break;
                case STRING:
                    if (str != null) {
                        dest.AppendString(str);
                        break;
                    }
                    dest.AppendShortString(lng, lng2);
                    break;
                case GUID:
                    dest.AppendGuid(Guid);
                    break;
                default:
                    throw new InvalidOperationException($"unexpected type in JsonKey.AppendTo()");
            }
        }
        
        public void AppendTo(StringBuilder sb) {
            switch (type) {
                case LONG:
                    sb.Append(lng);
                    break;
                case STRING:
                    if (str != null) {
                        sb.Append(str);
                        break;
                    }
                    Span<char> chars    = stackalloc char[MaxCharCount];
                    var len             = ShortStringUtils.GetChars(lng, lng2, chars);
                    var readOnlyChars   = chars.Slice(0, len);
                    sb.Append(readOnlyChars);
                    break;
                case GUID:
#if UNITY_5_3_OR_NEWER
                    var guidStr = Guid.ToString();
                    sb.Append(guidStr);
#else
                    Span<char> span = stackalloc char[Bytes.GuidLength];
                    if (!Guid.TryFormat(span, out int charsWritten))
                        throw new InvalidOperationException("AppendGuid() failed");
                    if (charsWritten != Bytes.GuidLength)
                        throw new InvalidOperationException($"Unexpected Guid length. Was: {charsWritten}");
                    sb.Append(span);
#endif
                    break;
                default:
                    throw new InvalidOperationException($"unexpected type in JsonKey.AppendTo()");
            }
        }
    }
    
    // ReSharper disable InconsistentNaming
    public enum JsonKeyType {
        NULL    = 0,
        LONG    = 1,
        STRING  = 2,
        GUID    = 3,
    }
}