// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public sealed class EntityValue
    {
        [Serialize] private     JsonValue   value;
        [Serialize] private     EntityError error;
        
        [Ignore]    public      JsonValue   Json    => error == null ? value : throw new EntityException(error);
        [Ignore]    public      EntityError Error   => error;

        public override         string      ToString() => error == null ? value.AsString() : error.type + ": " + error.message;

        public void SetJson(JsonValue json) {
            value = json;
        }
        
        /// <summary> Prefer using <see cref="SetJson(JsonValue)"/></summary>
        public void SetJson(string json) {
            value = new JsonValue(json);
        }
        
        public void SetError(EntityError error) {
            this.error = error;
            // assign "null". Invalid JSON cannot be serialized. As it is invalid, it cant be processed further anyway.
            value = new JsonValue();
        }

        public EntityValue() { } // required for TypeMapper

        
        public EntityValue(JsonValue json) {
            value = json;
        }
        
        /// <summary> Prefer using <see cref="EntityValue(JsonValue)"/> </summary>
        public EntityValue(string json) {
            value = new JsonValue(json);
        }
        
        public EntityValue(EntityError error) {
            this.error = error;
        }
    }
    

}