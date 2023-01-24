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
    public readonly struct JsonKey
    {
        internal readonly       JsonKeyType type;
        internal readonly       string      str;
        internal readonly       long        lng;    // also used as lower 64 bit for Guid
        [Browse(Never)]
        internal readonly       long        lng2;   // higher 64 bit for Guid
        
        public                  JsonKeyType Type    => type;
        internal                Guid        Guid    => GuidUtils.LongLongToGuid(lng, lng2);

        public   override       string      ToString()  { var value = AsString(); return value ?? "null"; }

        public static readonly  JsonKeyComparer         Comparer = new JsonKeyComparer();
        public static readonly  JsonKeyEqualityComparer Equality = new JsonKeyEqualityComparer();

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
            if (Guid.TryParse(value, out var guid)) {
                type    = GUID;
                str     = null;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = STRING;
            str     = value;
            lng     = 0;
            lng2    = 0;
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
            if (bytes.TryParseGuid(out var guid, out string temp)) { // temp not null in Unity. Otherwise null
                type    = GUID;
                str     = temp;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = STRING;
            if (oldKey.str == null) {
                str     = temp ?? bytes.AsString();
            } else {
                int len         = bytes.end - bytes.start;
                var src         = new ReadOnlySpan<byte>(bytes.buffer, bytes.start, len);
                var maxCount    = Encoding.UTF8.GetMaxCharCount(len);
                Span<char> dest = stackalloc char[maxCount];
                int strLen      = Encoding.UTF8.GetChars(src, dest);
                var newSpan     = dest.Slice(0, strLen);
                var oldSpan     = oldKey.str.AsSpan();
                if (newSpan.SequenceEqual(oldSpan)) {
                    str = oldKey.str;
                } else {
                    str = newSpan.ToString();
                }
            }
            lng     = 0;
            lng2    = 0;
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
                case LONG:      return false;
                case STRING:    return str == null;
                case GUID:      return false;
                case NULL:      return true;
                default:
                    throw new InvalidOperationException($"invalid JsonKey: {AsString()}");
            }
        }
        
        public bool IsEqual(in JsonKey other) {
            if (type != other.type)
                return false;
            
            switch (type) {
                case LONG:      return lng  == other.lng;
                case STRING:    return str  == other.str;
                case GUID:      return lng  == other.lng && lng2 == other.lng2;
                case NULL:      return true;
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }
        
        public int HashCode() {
            switch (type) {
                case LONG:      return lng. GetHashCode();
                case STRING:    return str. GetHashCode();
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

        /// <summary>Calling this method causes string instantiation. To avoid this use its <i>AppendTo</i> methods if possible.</summary> 
        public string AsString() {
            switch (type) {
                case LONG:      return str ?? lng. ToString();
                case STRING:    return str;
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
                    dest.AppendString(str);
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
                    sb.Append(str);
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