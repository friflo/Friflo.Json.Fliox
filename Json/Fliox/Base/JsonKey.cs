// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper;
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
                type    = JsonKeyType.Null;
                str     = null;
                lng     = 0;
                lng2    = 0;
                return;
            }
            if (long.TryParse(value, out long result)) {
                type    = JsonKeyType.Long;
                str     = null;
                lng     = result;
                lng2    = 0;
                return;
            }
            if (Guid.TryParse(value, out var guid)) {
                type    = JsonKeyType.Guid;
                str     = null;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = JsonKeyType.String;
            str     = value;
            lng     = 0;
            lng2    = 0;
        }
        
        public JsonKey (ref Bytes bytes, ref ValueParser valueParser) {
            if (bytes.IsIntegral()) {
                type    = JsonKeyType.Long;
                str     = null;
                var error = new Bytes();
                lng     = valueParser.ParseLong(ref bytes, ref error, out bool success);
                if (!success)
                    throw new InvalidOperationException("expect a valid integral type");
                lng2    = 0;
                return;
            }
            if (bytes.TryParseGuid(out var guid, out string temp)) { // temp not null in Unity. Otherwise null
                type    = JsonKeyType.Guid;
                str     = temp;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            type    = JsonKeyType.String;
            str     = temp ?? bytes.AsString();
            lng     = 0;
            lng2    = 0;
        }

        public JsonKey (long value) {
            type    = JsonKeyType.Long;
            str     = null;
            lng     = value;
            lng2    = 0;
        }
        
        public JsonKey (long? value) {
            type    = value.HasValue ? JsonKeyType.Long : JsonKeyType.Null;
            str     = null;
            lng     = value ?? 0;
            lng2    = 0;
        }
        
        public JsonKey (in Guid guid) {
            type    = JsonKeyType.Guid;
            str     = null;
            GuidUtils.GuidToLongLong(guid, out lng, out lng2);
        }
        
        public JsonKey (in Guid? guid) {
            var hasValue= guid.HasValue;
            type        = hasValue ? JsonKeyType.Guid : JsonKeyType.Null;
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
            type        = JsonKeyType.Null;
            str         = null;
            lng         = 0;
            guid        = new Guid();
        } */
        
        public bool IsNull() {
            switch (type) {
                case JsonKeyType.Long:      return false;
                case JsonKeyType.String:    return str == null;
                case JsonKeyType.Guid:      return false;
                case JsonKeyType.Null:      return true;
                default:
                    throw new InvalidOperationException($"invalid JsonKey: {AsString()}");
            }
        }
        
        public bool IsEqual(in JsonKey other) {
            if (type != other.type)
                return false;
            
            switch (type) {
                case JsonKeyType.Long:      return lng  == other.lng;
                case JsonKeyType.String:    return str  == other.str;
                case JsonKeyType.Guid:      return lng  == other.lng && lng2 == other.lng2;
                case JsonKeyType.Null:      return true;
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
                case JsonKeyType.Long:      return str ?? lng. ToString();
                case JsonKeyType.String:    return str;
                case JsonKeyType.Guid:      return str ?? Guid.ToString();
                case JsonKeyType.Null:      return null;
                default:
                    throw new InvalidOperationException($"unexpected type in JsonKey.AsString(). type: {type}");
            }
        }
        
        public long AsLong() {
            if (type == JsonKeyType.Long)
                return lng;
            throw new InvalidOperationException($"cannot return JsonKey as long. {AsString()}");
        }
        
        public Guid AsGuid() => Guid;
        
        public Guid? AsGuidNullable() {
            return type == JsonKeyType.Guid ? Guid : default; 
        }
        
        public void AppendTo(ref Bytes dest, ref ValueFormat valueFormat) {
            switch (type) {
                case JsonKeyType.Long:
                    valueFormat.AppendLong(ref dest, lng);
                    break;
                case JsonKeyType.String:
                    dest.AppendString(str);
                    break;
                case JsonKeyType.Guid:
                    dest.AppendGuid(Guid);
                    break;
                default:
                    throw new InvalidOperationException($"unexpected type in JsonKey.AppendTo()");
            }
        }
        
        public void AppendTo(StringBuilder sb) {
            switch (type) {
                case JsonKeyType.Long:
                    sb.Append(lng);
                    break;
                case JsonKeyType.String:
                    sb.Append(str);
                    break;
                case JsonKeyType.Guid:
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
    
    public enum JsonKeyType {
        Null    = 0,
        Long    = 1,
        String  = 2,
        Guid    = 3,
    }
}