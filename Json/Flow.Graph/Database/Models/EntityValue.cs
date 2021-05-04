// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Database.Models
{
    public class EntityValue
    {
        [Fri.Property]  private     JsonValue   value;
        [Fri.Ignore]    private     EntityError error;
        
        [Fri.Ignore]    public      string      Json    => error == null ? value.json : throw new EntityException(error);
        [Fri.Ignore]    public      EntityError Error   => error;

        public override             string      ToString() => Json;

        public void SetJson(string json) {
            value.json = json;
        }
        
        public void SetError(EntityError error) {
            this.error = error;
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