// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal partial class Set
    {
        internal  abstract  AggregateEntities   AggregateEntities   (AggregateTask      aggregate, in CreateTaskContext context);
        internal  abstract  CloseCursors        CloseCursors        (CloseCursorsTask   closeCursor);
        
        internal  abstract  void    ReserveKeysResult       (ReserveKeys        task, SyncTaskResult result);
        internal  abstract  void    CreateEntitiesResult    (CreateEntities     task, SyncTaskResult result);
        internal  abstract  void    UpsertEntitiesResult    (UpsertEntities     task, SyncTaskResult result);
        internal  abstract  void    ReadEntitiesResult      (ReadEntities       task, SyncTaskResult result);
        internal  abstract  void    QueryEntitiesResult     (QueryEntities      task, SyncTaskResult result);
        internal  abstract  void    AggregateEntitiesResult (AggregateEntities  task, SyncTaskResult result);
        internal  abstract  void    CloseCursorsResult      (CloseCursors       task, SyncTaskResult result);
        internal  abstract  void    PatchEntitiesResult     (MergeEntities      task, SyncTaskResult result);
        internal  abstract  void    DeleteEntitiesResult    (DeleteEntities     task, SyncTaskResult result);
        internal  abstract  void    SubscribeChangesResult  (SubscribeChanges   task, SyncTaskResult result);
        
        internal static string  SyncKeyName (string keyName)    => keyName == "id" ? null : keyName;
        internal static bool?   IsIntKey    (bool isIntKey)     => isIntKey ? true : null;
        
        internal static Dictionary<TKey, T> CreateDictionary<TKey, T>(int capacity = 0) {
            if (typeof(TKey) == typeof(JsonKey))
                return (Dictionary<TKey, T>)(object)new Dictionary<JsonKey, T>(capacity, JsonKey.Equality);
            if (typeof(TKey) == typeof(ShortString))
                return (Dictionary<TKey, T>)(object)new Dictionary<ShortString, T>(capacity, ShortString.Equality);
            return new Dictionary<TKey, T>(capacity);
        }
    }

    internal partial class Set<TKey, T>
    {
        internal override void ReserveKeysResult (ReserveKeys task, SyncTaskResult result) {
            var reserve = (ReserveKeysTask<TKey, T>)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                reserve.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var reserveKeysResult   = (ReserveKeysResult) result;
            if (!reserveKeysResult.keys.HasValue) {
                reserve.state.SetInvalidResponse("missing keys");
                return;
            }
            var resultKeys = reserveKeysResult.keys.Value;
            var count = resultKeys.count;
            var keys = new long[count];
            for (int n = 0; n < count; n++) {
                keys[n] =  resultKeys.start + n;
            }
            reserve.count           = count;
            reserve.keys            = keys;
            reserve.token           = resultKeys.token;
            reserve.state.Executed = true;
        }
        
        internal override void CreateEntitiesResult(CreateEntities task, SyncTaskResult result) {
            var createTask = (CreateTask<T>)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                createTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var createResult    = (CreateEntitiesResult)result;
            var writeErrors     = FlioxClient.ErrorsAsMap(createResult.errors, task.container);
            CreateUpsertEntitiesResult(task.entities, createTask, writeErrors);
        }

        internal override void UpsertEntitiesResult(UpsertEntities task, SyncTaskResult result) {
            var upsertTask = (UpsertTask<T>)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                upsertTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var upsertResult    = (UpsertEntitiesResult)result;
            var writeErrors     = FlioxClient.ErrorsAsMap(upsertResult.errors, task.container);
            CreateUpsertEntitiesResult(task.entities, upsertTask, writeErrors);
        }

        private void CreateUpsertEntitiesResult(
            List<JsonEntity>                    entities,
            WriteTask<T>                        writeTask,
            IDictionary<JsonKey, EntityError>   writeErrors)
        {
            if (TrackEntities) {
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    var id      = entity.key;
                    if (writeErrors.TryGetValue(id, out EntityError _)) {
                        continue;
                    }
                    var key = KeyConvert.IdToKey(id);
                    if (TryGetPeer(key, out var peer)) {
                        peer.state  = PeerState.None;
                        peer.SetPatchSource(entity.value);
                    }
                }
            }
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var entity in writeTask.entities) {
                if (writeErrors.TryGetValue(entity.key, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                writeTask.state.SetError(entityErrorInfo);
                return;
            }
            writeTask.state.Executed = true;
        }

        internal override void ReadEntitiesResult(ReadEntities task, SyncTaskResult result) {
            if (task.intern.syncTask is FindTask<TKey, T> readOneTask) {
                if (result is TaskErrorResult taskError) {
                    SetReadOneTaskError(readOneTask, taskError);
                    return;
                }
                ReadEntityResult(task, (ReadEntitiesResult)result, readOneTask);
            } else {
                var readTask    = (ReadTask<TKey,T>)task.intern.syncTask;
                if (result is TaskErrorResult taskError) {
                    SetReadTaskError(readTask, taskError);
                    return;
                }
                ReadEntitiesResult(task, (ReadEntitiesResult)result, readTask);
            }
        }

        internal override void QueryEntitiesResult(QueryEntities task, SyncTaskResult result) {
            var query   = (QueryTask<TKey, T>)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                var taskErrorInfo = new TaskErrorInfo(taskError);
                query.state.SetError(taskErrorInfo);
                SetSubRelationsError(query.relations.subRelations, taskErrorInfo);
                return;
            }
            var queryResult = (QueryEntitiesResult)result;
            QueryEntitiesResult(task, queryResult, query);
        }

        internal override void AggregateEntitiesResult (AggregateEntities task, SyncTaskResult result) {
            var aggregate   = (AggregateTask)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                aggregate.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var aggregateResult         = (AggregateEntitiesResult) result;
            aggregate.result            = aggregateResult.value;
            aggregate.state.Executed    = true;
        }
        
        internal override void CloseCursorsResult (CloseCursors task, SyncTaskResult result) {
            var closeCursor   = (CloseCursorsTask)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                closeCursor.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var closeResult             = (CloseCursorsResult) result;
            closeCursor.count           = closeResult.count; 
            closeCursor.state.Executed  = true;
        }

        internal override void PatchEntitiesResult(MergeEntities task, SyncTaskResult result)
        {
            var detectPatches   = task.intern.syncTask as DetectPatchesTask<TKey,T>;
            // ReSharper disable once PossibleNullReferenceException
            var patches         = detectPatches.patches;
            if (result is TaskErrorResult taskError)
            {
                detectPatches.state.SetError(new TaskErrorInfo(taskError));
                /* var patchErrors = new Dictionary<JsonKey, EntityError>(patches.Count, JsonKey.Equality);
                foreach (var patch in patches) {
                    var id      = KeyConvert.KeyToId(patch.Key); 
                    var error   = new EntityError(EntityErrorType.PatchError, set.nameShort, id, taskError.message){
                        taskErrorType   = taskError.type,
                        stacktrace      = taskError.stacktrace
                    };
                    patchErrors.TryAdd(id, error);
                } */
            } else {
                foreach (var entityPatch in patches) {
                    var key     = entityPatch.Key;
                    if (!TryGetPeer(key, out var peer)) {
                        continue;
                    }
                    var nextPatchSource = peer.NextPatchSource;
                    if (nextPatchSource.IsNull()) {
                        continue;
                    }
                    // set patch source (diff reference) only if entity is tracked
                    peer.SetPatchSource(nextPatchSource);
                    peer.SetNextPatchSourceNull();
                }
                var patchResult = (MergeEntitiesResult)result;
                var patchErrors = FlioxClient.ErrorsAsMap(patchResult.errors, task.container);
                detectPatches.SetResult(patchErrors);
            }
            // enable GC to collect references in containers which are not needed anymore
            patches.Clear();
        }

        internal override void DeleteEntitiesResult(DeleteEntities task, SyncTaskResult result) {
            if (task.intern.syncTask is DeleteAllTask<TKey,T> deleteAllTask) {
                deleteAllTask.state.Executed = true;
                return;
            }
            var deleteTask   = (DeleteTask<TKey,T>)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                deleteTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            if (TrackEntities) {
                if (task.all == true) {
                    DeletePeers();
                }
                var ids = task.ids;
                if (ids != null) {
                    foreach (var id in ids.GetReadOnlySpan()) {
                        DeletePeer(id);
                    }
                }
            }
            var deleteResult = (DeleteEntitiesResult)result;
            var errorsDelete = FlioxClient.ErrorsAsMap(deleteResult.errors, task.container);
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var key in deleteTask.keys) {
                var id = KeyConvert.KeyToId(key);
                if (errorsDelete.TryGetValue(id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                deleteTask.state.SetError(entityErrorInfo);
                return;
            }
            deleteTask.state.Executed = true;
        }
        
        internal override void SubscribeChangesResult (SubscribeChanges task, SyncTaskResult result) {
            var subscribeChanges   = (SubscribeChangesTask<T>)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                subscribeChanges.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            intern.subscription = task.changes.Count > 0 ? task : null;
            subscribeChanges.state.Executed = true;
        }
    }
}
