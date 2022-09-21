// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal static class FilterUtils
    {
        internal static SyncRequestTask FilterChanges (
            SyncRequestTask     task,
            SubscribeChanges    subscribe,
            JsonEvaluator       jsonEvaluator)
        {
            switch (task.TaskType) {
                
                case TaskType.create:
                    if (!subscribe.changes.Contains(EntityChange.create))
                        return null;
                    var create = (CreateEntities) task;
                    if (create.container != subscribe.container)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(subscribe.jsonFilter, create.entities, jsonEvaluator),
                        keyName     = create.keyName   
                    };
                    return createResult;
                
                case TaskType.upsert:
                    if (!subscribe.changes.Contains(EntityChange.upsert))
                        return null;
                    var upsert = (UpsertEntities) task;
                    if (upsert.container != subscribe.container)
                        return null;
                    var upsertResult = new UpsertEntities {
                        container   = upsert.container,
                        entities    = FilterEntities(subscribe.jsonFilter, upsert.entities, jsonEvaluator),
                        keyName     = upsert.keyName
                    };
                    return upsertResult;
                
                case TaskType.delete:
                    if (!subscribe.changes.Contains(EntityChange.delete))
                        return null;
                    var delete = (DeleteEntities) task;
                    if (subscribe.container != delete.container)
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.patch:
                    if (!subscribe.changes.Contains(EntityChange.patch))
                        return null;
                    var patch = (PatchEntities) task;
                    if (subscribe.container != patch.container)
                        return null;
                    // todo apply filter
                    return task;
                
                default:
                    return null;
            }
        }
        
        private static List<JsonValue> FilterEntities (
            JsonFilter          jsonFilter,
            List<JsonValue>     entities,
            JsonEvaluator       jsonEvaluator)    
        {
            if (jsonFilter == null)
                return entities;
            var result          = new List<JsonValue>();

            for (int n = 0; n < entities.Count; n++) {
                var value   = entities[n];
                if (jsonEvaluator.Filter(value, jsonFilter, out _)) {
                    result.Add(value);
                }
            }
            return result;
        }
    }
}