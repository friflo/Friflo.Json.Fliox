// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Sync
{
    public class EntityValue
    {
        [Fri.Property]  private     JsonValue   value;
        [Fri.Property]  private     EntityError error;
        
        [Fri.Ignore]    public      bool        validated;
        [Fri.Ignore]    public      string      Json    => error == null ? value.json : throw new EntityException(error);
        [Fri.Ignore]    public      EntityError Error   => error;

        public override             string      ToString() => error == null ? value.json : error.type + ": " + error.message;

        public void SetJson(string json) {
            value.json = json;
        }
        
        public void SetError(EntityError error) {
            this.error = error;
            // assign "null". Invalid JSON cannot be serialized. As it is invalid, it cant be processed further anyway.
            value.json = "null";
            validated = true;
        }

        public EntityValue() { } // required for TypeMapper

        public EntityValue(string json) {
            value.json = json;
        }
        
        public EntityValue(EntityError error) {
            this.error = error;
        }
    }
    

}