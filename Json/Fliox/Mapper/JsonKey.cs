// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper
{
    public readonly struct JsonKey
    {
        internal readonly   JsonKeyType type;
        internal readonly   string      str;
        internal readonly   long        lng;
        
        public override string ToString() => AsString();
        
        public static readonly JsonKeyComparer          Comparer = new JsonKeyComparer();
        public static readonly JsonKeyEqualityComparer  Equality = new JsonKeyEqualityComparer();

        public JsonKey (string str) {
            if (str == null) {
                type        = JsonKeyType.Null;
                this.str    = null;
                lng    = 0;
                return;
            }
            if (long.TryParse(str, out long result)) {
                this.type   = JsonKeyType.Long;
                this.str    = str;
                this.lng    = result;
                return;
            }
            this.type   = JsonKeyType.String;
            this.str    = str;
            this.lng    = 0;
        }
        
        public JsonKey (long lng) {
            this.type   = JsonKeyType.Long;
            this.str    = null;
            this.lng    = lng;
        }
        
        public JsonKey (long? lng) {
            this.type   = lng.HasValue ? JsonKeyType.Long : JsonKeyType.Null;
            this.str    = null;
            this.lng    = lng ?? 0;
        }
        
        public JsonKey (in Guid guid) {
            this.type   = JsonKeyType.String;
            this.str    = guid.ToString();
            this.lng    = 0;
        }
        
        public JsonKey (in Guid? guid) {
            var hasValue= guid.HasValue;
            type        = hasValue ? JsonKeyType.String : JsonKeyType.Null;
            str         = hasValue ? guid.ToString() : null;
            lng         = 0;
        }
        
        public bool IsNull() {
            switch (type) {
                case JsonKeyType.String:    return str == null;
                case JsonKeyType.Long:      return false;
                case JsonKeyType.Null:      return true;
                default:
                    throw new InvalidOperationException($"invalid JsonKey: {ToString()}");
            }
        }
        public bool IsEqual(in JsonKey other) {
            if (type != other.type)
                return false;
            
            switch (type) {
                case JsonKeyType.String:    return str == other.str;
                case JsonKeyType.Long:      return lng == other.lng;
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

        public string AsString() {
            switch (type) {
                case JsonKeyType.String:    return str;
                case JsonKeyType.Long:      return str ?? lng.ToString();
                case JsonKeyType.Null:      return null;
                default:                    return "None";
            }
        }
        
        public long AsLong() {
            if (type == JsonKeyType.Long)
                return lng;
            throw new InvalidOperationException($"cannot return JsonKey as long. {ToString()}");
        }
        
        public Guid AsGuid() {
            return new Guid(AsString());
        }
        
        public Guid? AsGuidNullable() {
            return str != null ? new Guid(str) : default;
        }
    }
    
    public enum JsonKeyType {
        Null,
        String,
        Long,
    }
}