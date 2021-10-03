// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Fliox.Mapper
{
    public readonly struct JsonKey
    {
        internal readonly       JsonKeyType type;
        internal readonly       string      str; // todo could be mutable to cache ToString() results got lng & guid
        internal readonly       long        lng;
        internal readonly       Guid        guid;
        
        public   override       string      ToString() => AsString();
        
        public static readonly  JsonKeyComparer         Comparer = new JsonKeyComparer();
        public static readonly  JsonKeyEqualityComparer Equality = new JsonKeyEqualityComparer();

        /// Calling this constructor should be the last option as it may force a string creation.
        /// Use alternative constructors is input type is already refined.
        public JsonKey (string str)
        {
            if (str == null) {
                type        = JsonKeyType.Null;
                this.str    = null;
                lng         = 0;
                guid        = new Guid();
                return;
            }
            if (long.TryParse(str, out long result)) {
                this.type   = JsonKeyType.Long;
                this.str    = str;
                this.lng    = result;
                guid        = new Guid();
                return;
            }
            if (Guid.TryParse(str, out guid)) {
                type   = JsonKeyType.Guid;
            } else {
                type   = JsonKeyType.String;
            }
            this.str    = str;
            this.lng    = 0;
        }
        
        public JsonKey (ref Bytes bytes, ref ValueParser valueParser) {
            if (bytes.IsIntegral()) {
                this.type   = JsonKeyType.Long;
                this.str    = null;
                var error = new Bytes();
                this.lng    = valueParser.ParseLong(ref bytes, ref error, out bool success);
                if (!success)
                    throw new InvalidOperationException("expect a valid integral type");
                guid        = new Guid();
                return;
            }
            if (bytes.TryParseGuid(out guid, out string temp)) { // temp not null in Unity. Otherwise null
                type    = JsonKeyType.Guid;
                str     = temp;
            } else {
                type    = JsonKeyType.String;
                str     = temp ?? bytes.AsString();
            }
            this.lng    = 0;
        }

        public JsonKey (long lng) {
            this.type   = JsonKeyType.Long;
            this.str    = null;
            this.lng    = lng;
            this.guid   = new Guid();
        }
        
        public JsonKey (long? lng) {
            this.type   = lng.HasValue ? JsonKeyType.Long : JsonKeyType.Null;
            this.str    = null;
            this.lng    = lng ?? 0;
            this.guid   = new Guid();
        }
        
        public JsonKey (in Guid guid) {
            this.type   = JsonKeyType.Guid;
            this.str    = null;
            this.lng    = 0;
            this.guid   = guid;
        }
        
        public JsonKey (in Guid? guid) {
            var hasValue= guid.HasValue;
            type        = hasValue ? JsonKeyType.Guid : JsonKeyType.Null;
            str         = null; // hasValue ? guid.ToString() : null;
            lng         = 0;
            this.guid   = hasValue ? guid.Value : new Guid();
        }
        
        public JsonKey (in JsonKey? jsonKey) {
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
        }
        
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
                case JsonKeyType.Guid:      return guid == other.guid;
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

        /// Calling should be avoided is possible use <see cref="AppendTo"/> methods. 
        public string AsString() {
            switch (type) {
                case JsonKeyType.Long:      return str ?? lng. ToString();
                case JsonKeyType.String:    return str;
                case JsonKeyType.Guid:      return str ?? guid.ToString();
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
        
        public Guid AsGuid() {
            return guid;
        }
        
        public Guid? AsGuidNullable() {
            return type == JsonKeyType.Guid ? guid : default; 
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
                    dest.AppendGuid(guid);
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
                    var guidStr = guid.ToString();
                    sb.Append(guidStr);
#else
                    Span<char> span = stackalloc char[Bytes.MinGuidLength];
                    if (!guid.TryFormat(span, out int charsWritten))
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
        Null,
        Long,
        String,
        Guid,
    }
}