// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

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
                
                case TaskType.create: {
                    if ((subscribe.changes & EntityChange.create) == 0)
                        return null;
                    var create = (CreateEntities) task;
                    if (!create.entityContainer.nameShort.IsEqual(subscribe.container))
                        return null;
                    var filter = subscribe.jsonFilter;
                    if (filter == null)
                        return create;
                    var entities    = FilterEntities(filter, create.entities, jsonEvaluator);
                    return new CreateEntities {
                        container   = create.container,
                        entities    = entities,
                        keyName     = create.keyName   
                    };
                }
                case TaskType.upsert: {
                    if ((subscribe.changes & EntityChange.upsert) == 0)
                        return null;
                    var upsert = (UpsertEntities) task;
                    if (!upsert.entityContainer.nameShort.IsEqual(subscribe.container))
                        return null;
                    // if (!IsEventTarget(subClient, upsert.users))
                    //    return null;
                    var filter = subscribe.jsonFilter;
                    if (filter == null)
                        return upsert;
                    var entities    = FilterEntities(filter, upsert.entities, jsonEvaluator);
                    return new UpsertEntities {
                        container   = upsert.container,
                        entities    = entities,
                        keyName     = upsert.keyName
                    };
                }
                case TaskType.delete:
                    if ((subscribe.changes & EntityChange.delete) == 0)
                        return null;
                    var delete = (DeleteEntities) task;
                    if (!delete.entityContainer.nameShort.IsEqual(subscribe.container))
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.merge:
                    if ((subscribe.changes & EntityChange.merge) == 0)
                        return null;
                    var merge = (MergeEntities) task;
                    if (!merge.entityContainer.nameShort.IsEqual(subscribe.container))
                        return null;
                    //if (!IsEventTarget(subClient, merge.users))
                    //    return null;
                    // todo apply filter
                    return task;
                
                default:
                    return null;
            }
        }
        
        /*
        private static bool IsEventTarget (EventSubClient subClient, List<JsonKey> targetUsers) {
            if (targetUsers == null)
                return true;
            var subUser = subClient.user;
            foreach (var targetUser in targetUsers) {
                if (subUser.userId.IsEqual(targetUser))
                    return true;
            }
            return false;
        } */
        
        private static List<JsonEntity> FilterEntities (
            JsonFilter          jsonFilter,
            List<JsonEntity>    entities,
            JsonEvaluator       jsonEvaluator)    
        {
            var result  = new List<JsonEntity>();
            foreach (var entity in entities) {
                if (jsonEvaluator.Filter(entity.value, jsonFilter, out _)) {
                    result.Add(entity);
                }
            }
            return result;
        }
    }
}