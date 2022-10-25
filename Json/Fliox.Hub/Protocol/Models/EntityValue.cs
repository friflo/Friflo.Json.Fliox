// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public sealed class EntityValue
    {
        [Ignore]    internal    JsonKey     key;
        [Serialize] private     JsonValue   value;
        [Serialize] private     EntityError error;
        
        [Ignore]    public      JsonKey     Key     => key;
        [Ignore]    public      JsonValue   Json    => error == null ? value : throw new EntityException(error);
        [Ignore]    public      EntityError Error   => error;

        public override         string      ToString() => error == null ? value.AsString() : error.type + ": " + error.message;

        public void SetJson(in JsonKey key, JsonValue json) {
            this.key    = key;
            value       = json;
        }
        
        /// <summary> Prefer using SetJson(in JsonKey key, JsonValue json)</summary>
        public void SetJson(in JsonKey key, string json) {
            this.key    = key;
            value       = new JsonValue(json);
        }
        
        public void SetError(in JsonKey key, EntityError error) {
            this.error = error;
            // assign "null". Invalid JSON cannot be serialized. As it is invalid, it cant be processed further anyway.
            value = new JsonValue();
        }

        public EntityValue() { } // required for TypeMapper

        public EntityValue(in JsonKey key) {
            this.key    = key;
        }

        public EntityValue(in JsonKey key, JsonValue json) {
            this.key    = key;
            value       = json;
        }
        
        /// <summary> Prefer using EntityValue(in JsonKey key, JsonValue json) </summary>
        public EntityValue(in JsonKey key, string json) {
            this.key    = key;
            value       = new JsonValue(json);
        }
        
        public EntityValue(in JsonKey key, EntityError error) {
            this.key    = key;
            this.error  = error;
        }
    }
    

}