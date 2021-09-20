// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    public class EntityValue
    {
        [Fri.Property]  private     JsonValue   value;
        [Fri.Property]  private     EntityError error;
        
        [Fri.Ignore]    public      Utf8Json    Json    => error == null ? value.json : throw new EntityException(error);
        [Fri.Ignore]    public      EntityError Error   => error;

        public override             string      ToString() => error == null ? value.json.AsString() : error.type + ": " + error.message;

        public void SetJson(Utf8Json json) {
            value.json = json;
        }
        
        /// <summary> Prefer using <see cref="SetJson(Utf8Json)"/></summary>
        public void SetJson(string json) {
            value.json = new Utf8Json(json);
        }
        
        public void SetError(EntityError error) {
            this.error = error;
            // assign "null". Invalid JSON cannot be serialized. As it is invalid, it cant be processed further anyway.
            value.json = new Utf8Json();
        }

        public EntityValue() { } // required for TypeMapper

        
        public EntityValue(Utf8Json json) {
            value.json = json;
        }
        
        /// <summary> Prefer using <see cref="EntityValue(Utf8Json)"/> </summary>
        public EntityValue(string json) {
            value.json = new Utf8Json(json);
        }
        
        public EntityValue(EntityError error) {
            this.error = error;
        }
    }
    

}