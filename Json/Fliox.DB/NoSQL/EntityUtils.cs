// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    public static class EntityUtils
    {
        /// <summary>
        /// Add the given <see cref="entities"/> to the given <see cref="entityMap"/>.
        /// The given <see cref="keyName"/> identifies the key property inside the JSON value in the given list of <see cref="entities"/>.
        /// </summary>
        public static void AddEntitiesToMap(
            List<JsonValue>                     entities,
            string                              keyName,
            Dictionary<JsonKey, EntityValue>    entityMap,
            MessageContext                      messageContext)
        {
            using (var pooledValidator = messageContext.pools.EntityValidator.Get()) {
                var validator = pooledValidator.instance;
                foreach (var entity in entities) {
                    if (!validator.GetEntityKey(entity.json, ref keyName, out JsonKey keyValue, out _)) {
                        continue;
                    }
                    var value   = new EntityValue(entity.json);
                    entityMap.Add(keyValue, value);
                }
            }
        }
        
        internal static List<JsonKey> CreateEntityKeys (
            string                                  keyName,
            List<JsonValue>                         entities,
            MessageContext                          messageContext,
            out string                              error
        ) {
            var keys = new List<JsonKey>(entities.Count);
            using (var poolValidator = messageContext.pools.EntityValidator.Get()) {
                var validator = poolValidator.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    if (validator.GetEntityKey(entity.json, ref keyName, out JsonKey key, out string entityError)) {
                        keys.Add(key);
                        continue;
                    }
                    error = $"error at entities[{n}]: {entityError}";
                    return null;
                }
            }
            EntityContainer.AssertEntityCounts(keys, entities);
            error = null;
            return keys;
        }
    }
}