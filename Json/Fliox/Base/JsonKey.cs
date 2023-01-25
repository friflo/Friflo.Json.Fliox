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
        private const           int                     GuidLength  = 36;

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
            if (stringLength == GuidLength && Guid.TryParse(value, out var guid)) {
                type    = GUID;
                str     = null;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = STRING;
            ShortString.StringToLongLong(value, out str, out lng, out lng2);
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
            if (bytesLen == GuidLength && bytes.TryParseGuid(out var guid, out string temp)) { // temp not null in Unity. Otherwise null
                type    = GUID;
                str     = temp;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = STRING;
            if (ShortString.BytesToLongLong(bytes, out lng, out lng2)) {
                str     = null;
            } else {
                str     = GetString(bytes, oldKey.str);
                lng     = 0;
                lng2    = 0;
            }
        }
        
        private static string GetString(in Bytes bytes, string oldKey) {
            int len         = bytes.end - bytes.start;
            var src         = new ReadOnlySpan<byte>(bytes.buffer, bytes.start, len);
            var maxCount    = Encoding.UTF8.GetMaxCharCount(len);
            Span<char> dest = stackalloc char[maxCount];
            int strLen      = Encoding.UTF8.GetChars(src, dest);
            var newSpan     = dest.Slice(0, strLen);
            var oldSpan     = oldKey.AsSpan();
            if (newSpan.SequenceEqual(oldSpan)) {
                return oldKey;
            }
            return newSpan.ToString();
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
        
    /*  public JsonKey (in JsonKey? jsonKey) {
            if (jsonKey.HasValue) {
                var value = jsonKey.Value; 
                type        = value.type;
                str         = value.str;
                lng         = value.lng;
                guid        = value.guid;
                return;
            }
            type        = NULL;
            str         = null;
            lng         = 0;
            guid        = new Guid();
        } */
        
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
        
        public static int StringCompare(in JsonKey left, in JsonKey right, StringComparison comparisonType)
        {
            if (left.type   != STRING) throw new ArgumentException("expect left.type: STRING");
            if (right.type  != STRING) throw new ArgumentException("expect right.type: STRING");
            
            if (right.str != null) {
                if (left.str != null) {
                    return string.Compare(left.str, right.str, comparisonType);
                }
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = ShortString.GetChars(left.lng, left.lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.CompareTo(rightReadOnly, comparisonType);
            }
            // case: right.str == null
            if (left.str != null) {
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = ShortString.GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = left.str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, comparisonType);
            } else {
                Span<char> leftChars    = stackalloc char[MaxCharCount];
                var leftCount           = ShortString.GetChars(left.lng, left.lng2, leftChars);
                
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = ShortString.GetChars(right.lng, right.lng2, rightChars);

                ReadOnlySpan<char>  leftReadOnly    = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char>  rightReadOnly   = rightChars. Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, comparisonType);
            }
        }
        
        public static bool StringStartsWith(in JsonKey left, in JsonKey right, StringComparison comparisonType)
        {
            if (left.type   != STRING) throw new ArgumentException("expect left.type: STRING");
            if (right.type  != STRING) throw new ArgumentException("expect right.type: STRING");
            
            if (right.str != null) {
                if (left.str != null) {
                    return left.str.StartsWith(right.str, comparisonType);
                }
                // --- case: only left is short string
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = ShortString.GetChars(left.lng, left.lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.StartsWith(rightReadOnly, comparisonType);
            }
            // --- case: right.str == null  =>  only right is short string
            int rightLength = (int)(right.lng2 >> ShortString.ShiftLength);
            if (rightLength == 0) {
                return true;    // early out for right: ""
            }
            if (left.str != null) {
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = ShortString.GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = left.str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.StartsWith(rightReadOnly, comparisonType);
            }
            // --- case: left and right are short strings
            int leftLength = (int)(left.lng2 >> ShortString.ShiftLength);
            if (rightLength > leftLength) {
                return false;
            }
            if (rightLength < 8) {
                long mask0  = 0x00ff_ffff_ffff_ffff >> (8 * (    7 - rightLength));
                return  (left.lng & mask0)  == (right.lng & mask0);
            } else {
                long mask8  = 0x00ff_ffff_ffff_ffff >> (8 * (8 + 7 - rightLength));
                return   left.lng           ==  right.lng &&
                        (left.lng2 & mask8) == (right.lng2 & mask8);
            }
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
                    var length          = ShortString.GetChars(lng, lng2, chars);
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
                    var len             = ShortString.GetChars(lng, lng2, chars);
                    var readOnlyChars   = chars.Slice(0, len);
                    sb.Append(readOnlyChars);
                    break;
                case GUID:
#if UNITY_5_3_OR_NEWER
                    var guidStr = Guid.ToString();
                    sb.Append(guidStr);
#else
                    Span<char> span = stackalloc char[Bytes.MinGuidLength];
                    if (!Guid.TryFormat(span, out int charsWritten))
                        throw new InvalidOperationException("AppendGuid() failed");
                    if (charsWritten != Bytes.MinGuidLength)
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