// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    public static class EntityUtils
    {
        /// <summary>
        /// Add the given <paramref name="entities"/> to the given <paramref name="entityMap"/>.
        /// The given <paramref name="keyName"/> identifies the key property inside the JSON value in the given list of <paramref name="entities"/>.
        /// </summary>
        public static void AddEntitiesToMap(
            List<JsonValue>                     entities,
            string                              keyName,
            bool?                               isIntKey,
            string                              newKeyName,
            Dictionary<JsonKey, EntityValue>    entityMap,
            SyncContext                         syncContext)
        {
            var asIntKey = isIntKey == true;
            using (var pooled = syncContext.EntityProcessor.Get()) {
                var processor = pooled.instance;
                foreach (var entity in entities) {
                    var  json = processor.ReplaceKey(entity, keyName, asIntKey, newKeyName, out JsonKey keyValue, out _);
                    if (json.IsNull()) {
                        continue;
                    }
                    var value   = new EntityValue(json);
                    entityMap.Add(keyValue, value);
                }
            }
        }
        
        public static Task<JsonValue> ReadToEnd(Stream input) {
            byte[] buffer = new byte[16 * 1024];                // todo performance -> cache
            using (MemoryStream ms = new MemoryStream()) {      // todo performance -> cache
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0) {
                    ms.Write(buffer, 0, read);
                }
                var array = ms.ToArray(); 
                return Task.FromResult(new JsonValue(array));
            }
        }
        
        internal static bool GetKeysFromEntities (
            string              keyName,
            List<JsonEntity>    entities,
            SyncContext         syncContext,
            out string          error
        ) {
            using (var pooled = syncContext.EntityProcessor.Get()) {
                var processor = pooled.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    if (processor.GetEntityKey(entity.value, keyName, out JsonKey key, out string entityError)) {
                        entities[n] = new JsonEntity(key, entity.value);
                        continue;
                    }
                    error = $"error at entities[{n}]: {entityError}";
                    return false;
                }
            }
            error = null;
            return true;
        }
    }
}