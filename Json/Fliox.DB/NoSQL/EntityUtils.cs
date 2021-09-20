// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
            bool?                               isIntKey,
            string                              newKeyName,
            Dictionary<JsonKey, EntityValue>    entityMap,
            MessageContext                      messageContext)
        {
            var asIntKey = isIntKey == true;
            using (var pooledProcessor = messageContext.pools.EntityProcessor.Get()) {
                var processor = pooledProcessor.instance;
                foreach (var entity in entities) {
                    var  json = processor.ReplaceKey(entity.json, keyName, asIntKey, newKeyName, out JsonKey keyValue, out _);
                    if (json.IsNull()) {
                        continue;
                    }
                    var value   = new EntityValue(json);
                    entityMap.Add(keyValue, value);
                }
            }
        }
        
        public static Task<JsonUtf8> ReadToEnd(Stream input) {
            byte[] buffer = new byte[16 * 1024];                // todo performance -> cache
            using (MemoryStream ms = new MemoryStream()) {      // todo performance -> cache
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0) {
                    ms.Write(buffer, 0, read);
                }
                var array = ms.ToArray(); 
                return Task.FromResult(new JsonUtf8(array));
            }
        }
        
        internal static List<JsonKey> GetKeysFromEntities (
            string              keyName,
            List<JsonValue>     entities,
            MessageContext      messageContext,
            out string          error
        ) {
            var keys = new List<JsonKey>(entities.Count);
            using (var poolProcessor = messageContext.pools.EntityProcessor.Get()) {
                var processor = poolProcessor.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    if (processor.GetEntityKey(entity.json, keyName, out JsonKey key, out string entityError)) {
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