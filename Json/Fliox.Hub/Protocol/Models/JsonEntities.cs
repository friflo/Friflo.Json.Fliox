// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    // Currently not used - intended to replace usage of Dictionary<JsonKey, EntityValue> types
    [TypeMapper(typeof(JsonEntitiesMatcher))]
    public struct JsonEntities
    {
        public Dictionary<JsonKey, EntityValue> entities;
        
        public JsonEntities(int capacity) {
            entities = new Dictionary<JsonKey, EntityValue>(capacity, JsonKey.Equality);
        }
        
        public void Init(int capacity) {
            if (entities == null) {
                entities = new Dictionary<JsonKey, EntityValue>(capacity, JsonKey.Equality);
                return;
            }
            entities.EnsureCapacity(capacity);
        }
    }
}