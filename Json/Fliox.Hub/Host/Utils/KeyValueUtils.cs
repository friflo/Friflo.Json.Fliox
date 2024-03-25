// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    public static class KeyValueUtils
    {
        /// <summary>
        /// Copy the given <paramref name="entities"/> to the given <paramref name="destEntities"/>.
        /// The given <paramref name="keyName"/> identifies the key property inside the JSON value in the given list of <paramref name="entities"/>.
        /// </summary>
        public static void CopyEntities(
            List<JsonValue>     entities,
            string              keyName,
            bool?               isIntKey,
            string              newKeyName,
            List<EntityValue>   destEntities,
            SyncContext         syncContext)
        {
            var asIntKey = isIntKey == true;
            using (var pooled = syncContext.EntityProcessor.Get()) {
                var processor = pooled.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity = entities[n];
                    var  json = processor.ReplaceKey(entity, keyName, asIntKey, newKeyName, out JsonKey keyValue, out _);
                    if (json.IsNull()) {
                        continue;
                    }
                    destEntities.Add(new EntityValue(keyValue, json));
                }
            }
        }
        
        public static async Task<JsonValue> ReadToEndAsync(Stream input, StreamBuffer buffer) {
            buffer.Position = 0;
            int read;
            while ((read = await input.ReadAsync(buffer.GetBuffer(), buffer.Position, buffer.Remaining).ConfigureAwait(false)) > 0) {
                buffer.Position += read;
                if (buffer.Remaining > 0)
                    continue;
                buffer.SetCapacity(2 * buffer.Capacity);
            }
            return new JsonValue(buffer.GetBuffer(), 0, buffer.Position);
        }
        
        public static JsonValue CreateCopy(in JsonValue value, MemoryBuffer buffer) {
            if (value.Count < 4096)
                return buffer.Add(value);
            return new JsonValue(value);
        }
        
        internal static bool GetKeysFromEntities (
            string              keyName,
            List<JsonEntity>    entities,
            SharedEnv           env,
            out string          error
        ) {
            if (entities.Count == 0 || !entities[0].key.IsNull()) {
                // early out: in case of direct (!= remote) execution keys are set by the client
                error = null;
                return true;
            }
            using (var pooled = env.pool.EntityProcessor.Get()) {
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
        
        internal static void OrderKeys (ListOne<JsonKey> keys, SortOrder? order) {
            if (order == null)
                return;
            keys.Sort(JsonKey.Comparer);
            if (order == SortOrder.desc) {
                keys.Reverse();
            }
        }
        
        internal static void OrderKeys (List<JsonKey> keys, SortOrder? order) {
            if (order == null)
                return;
            keys.Sort(JsonKey.Comparer);
            if (order == SortOrder.desc) {
                keys.Reverse();
            }
        }
    }
}