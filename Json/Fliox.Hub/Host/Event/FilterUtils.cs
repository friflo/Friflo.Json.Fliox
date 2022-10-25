// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal static class FilterUtils
    {
        internal static SyncRequestTask FilterChanges (
            EventSubClient  subClient, 
            SyncRequestTask task,
            in ChangeSub    subscribe,
            JsonEvaluator   jsonEvaluator)
        {
            switch (task.TaskType) {
                
                case TaskType.create:
                    if (Array.IndexOf(subscribe.changes, EntityChange.create) == -1)
                        return null;
                    var create = (CreateEntities) task;
                    if (create.container != subscribe.container)
                        return null;
                    var entities = FilterEntities(subscribe.jsonFilter, create.entities, jsonEvaluator);
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = entities,
                        keyName     = create.keyName   
                    };
                    return createResult;
                
                case TaskType.upsert:
                    if (Array.IndexOf(subscribe.changes, EntityChange.upsert) == -1)
                        return null;
                    var upsert = (UpsertEntities) task;
                    if (upsert.container != subscribe.container)
                        return null;
                    if (!IsEventTarget(subClient, upsert.users))
                        return null;
                    entities = FilterEntities(subscribe.jsonFilter, upsert.entities, jsonEvaluator);
                    var upsertResult = new UpsertEntities {
                        container   = upsert.container,
                        entities    = entities,
                        keyName     = upsert.keyName
                    };
                    return upsertResult;
                
                case TaskType.delete:
                    if (Array.IndexOf(subscribe.changes, EntityChange.delete) == -1)
                        return null;
                    var delete = (DeleteEntities) task;
                    if (subscribe.container != delete.container)
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.merge:
                    if (Array.IndexOf(subscribe.changes, EntityChange.merge) == -1)
                        return null;
                    var merge = (MergeEntities) task;
                    if (subscribe.container != merge.container)
                        return null;
                    if (!IsEventTarget(subClient, merge.users))
                        return null;
                    // todo apply filter
                    return task;
                
                default:
                    return null;
            }
        }
        
        private static bool IsEventTarget (EventSubClient subClient, List<JsonKey> targetUsers) {
            if (targetUsers == null)
                return true;
            var subUser = subClient.user;
            foreach (var targetUser in targetUsers) {
                if (subUser.userId.IsEqual(targetUser))
                    return true;
            }
            return false;
        }
        
        private static List<JsonEntity> FilterEntities (
            JsonFilter          jsonFilter,
            List<JsonEntity>    entities,
            JsonEvaluator       jsonEvaluator)    
        {
            if (jsonFilter == null)
                return entities;
            var result          = new List<JsonEntity>();

            for (int n = 0; n < entities.Count; n++) {
                var entity   = entities[n];
                if (jsonEvaluator.Filter(entity.value, jsonFilter, out _)) {
                    result.Add(entity);
                }
            }
            return result;
        }
    }
}