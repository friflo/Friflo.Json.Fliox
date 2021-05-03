// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Database.Models
{
    public class EntityValue
    {
        [Fri.Property]
        private JsonValue   value;

        [Fri.Ignore]
        public string Json => value.json;
        
        public void SetJson(string json) {
            value.json = json;
        }

        public EntityValue() { } // required for TypeMapper

        public EntityValue(string json) {
            value.json = json;
        }
    }
}