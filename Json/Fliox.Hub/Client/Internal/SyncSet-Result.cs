// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal abstract class SyncSet
    {
        internal static readonly IDictionary<JsonKey, EntityError> NoErrors = new EmptyDictionary<JsonKey, EntityError>();  
            
        internal    IDictionary<JsonKey, EntityError>    errorsCreate = NoErrors;
        internal    IDictionary<JsonKey, EntityError>    errorsUpsert = NoErrors;
        internal    IDictionary<JsonKey, EntityError>    errorsPatch  = NoErrors;
        internal    IDictionary<JsonKey, EntityError>    errorsDelete = NoErrors;

        internal  abstract  EntitySet           EntitySet   { get; }

        internal  abstract  AggregateEntities   AggregateEntities   (AggregateTask      aggregate, in CreateTaskContext context);
        internal  abstract  CloseCursors        CloseCursors        (CloseCursorsTask   closeCursor);
        
        internal  abstract  void    ReserveKeysResult       (ReserveKeys        task, SyncTaskResult result);
        internal  abstract  void    CreateEntitiesResult    (CreateEntities     task, SyncTaskResult result, ObjectMapper mapper);
        internal  abstract  void    UpsertEntitiesResult    (UpsertEntities     task, SyncTaskResult result, ObjectMapper mapper);
        internal  abstract  void    ReadEntitiesResult      (ReadEntities       task, SyncTaskResult result, ContainerEntities readEntities);
        internal  abstract  void    QueryEntitiesResult     (QueryEntities      task, SyncTaskResult result, ContainerEntities queryEntities);
        internal  abstract  void    AggregateEntitiesResult (AggregateEntities  task, SyncTaskResult result);
        internal  abstract  void    CloseCursorsResult      (CloseCursors       task, SyncTaskResult result);
        internal  abstract  void    PatchEntitiesResult     (PatchEntities      task, SyncTaskResult result);
        internal  abstract  void    DeleteEntitiesResult    (DeleteEntities     task, SyncTaskResult result);
        internal  abstract  void    SubscribeChangesResult  (SubscribeChanges   task, SyncTaskResult result);
        
        internal static string SyncKeyName (string keyName) {
            if (keyName == "id")
                return null;
            return keyName;
        }
        
        internal static bool? IsIntKey (bool isIntKey) {
            if (isIntKey)
                return true;
            return null;
        }
        
        internal static HashSet<TKey> CreateHashSet<TKey>(int capacity = 0) {
            if (typeof(TKey) == typeof(JsonKey))
                return (HashSet<TKey>)(object)Helper.CreateHashSet(capacity, JsonKey.Equality);
            return Helper.CreateHashSet<TKey>(capacity);
        }
        
        internal static Dictionary<TKey, T> CreateDictionary<TKey, T>(int capacity = 0) {
            if (typeof(TKey) == typeof(JsonKey))
                return (Dictionary<TKey, T>)(object)new Dictionary<JsonKey, T>(capacity, JsonKey.Equality);
            return new Dictionary<TKey, T>(capacity);
        }
    }

    internal partial class SyncSet<TKey, T>
    {
        internal override void ReserveKeysResult (ReserveKeys task, SyncTaskResult result) {
            var reserve = (ReserveKeysTask<TKey, T>)task.syncTask;
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
        
        /// In case of a <see cref="TaskErrorResult"/> add entity errors to <see cref="SyncSet.errorsCreate"/> for all
        /// <see cref="WriteTask{T}.peers"/> to enable setting <see cref="DetectPatchesTask"/> to error state via <see cref="DetectPatchesTask{T}.SetResult"/>. 
        internal override void CreateEntitiesResult(CreateEntities task, SyncTaskResult result, ObjectMapper mapper) {
            var createTask = (CreateTask<T>)task.syncTask;
            CreateUpsertEntitiesResult(task.entityKeys, task.entities, result, createTask, errorsCreate, mapper);
            var creates = createTask.peers;
            if (result is TaskErrorResult taskError) {
                if (errorsCreate == NoErrors) {
                    errorsCreate = new Dictionary<JsonKey, EntityError>(creates.Count, JsonKey.Equality);
                }
                foreach (var createPair in creates) {
                    var id = createPair.Key;
                    var error = new EntityError(EntityErrorType.WriteError, set.name, id, taskError.message) {
                        taskErrorType   = taskError.type,
                        stacktrace      = taskError.stacktrace
                    };
                    errorsCreate.TryAdd(id, error);
                }
            }
        }

        internal override void UpsertEntitiesResult(UpsertEntities task, SyncTaskResult result, ObjectMapper mapper) {
            var upsertTask = (UpsertTask<T>)task.syncTask;
            CreateUpsertEntitiesResult(task.entityKeys, task.entities, result, upsertTask, errorsUpsert, mapper);
        }

        private void CreateUpsertEntitiesResult(
            List<JsonKey>                       keys,
            List<JsonValue>                     entities,
            SyncTaskResult                      result,
            WriteTask<T>                        writeTask,
            IDictionary<JsonKey, EntityError>   writeErrors,
            ObjectMapper                        mapper)
        {
            if (result is TaskErrorResult taskError) {
                writeTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            if (keys.Count != entities.Count)
                throw new InvalidOperationException("Expect equal counts");
            var reader = mapper.reader;
            for (int n = 0; n < entities.Count; n++) {
                var entity = entities[n];
                // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                var id = keys[n];
                if (writeErrors.TryGetValue(id, out EntityError _)) {
                    continue;
                }
                var key     = KeyConvert.IdToKey(id);
                var peer    = set.GetOrCreatePeerByKey(key, id);
                peer.state  = PeerState.None;
                peer.SetPatchSource(reader.Read<T>(entity));
            }

            var entityErrorInfo = new TaskErrorInfo();
            var idsBuf = set.intern.store._intern.IdsBuf();
            idsBuf.Clear();
            writeTask.GetIds(idsBuf);
            foreach (var id in idsBuf) {
                if (writeErrors.TryGetValue(id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                writeTask.state.SetError(entityErrorInfo);
                return;
            }
            writeTask.state.Executed = true;
        }

        internal override void ReadEntitiesResult(ReadEntities task, SyncTaskResult result, ContainerEntities readEntities) {
            var readTask    = (ReadTask<TKey,T>)task.syncTask;
            if (result is TaskErrorResult taskError) {
                SetReadTaskError(readTask, taskError);
                return;
            }
            var readResult = (ReadEntitiesResult) result;
            ReadEntitiesResult(task, readResult, readTask, readEntities);
        }

        private void ReadEntitiesResult(ReadEntities task, ReadEntitiesResult result, ReadTask<TKey, T> read, ContainerEntities readEntities) {
            if (result.Error != null) {
                var taskError = SyncRequestTask.TaskError(result.Error);
                SetReadTaskError(read, taskError);
                return;
            }
            var entityErrorInfo = new TaskErrorInfo();
            var entities = readEntities.entityMap;
            foreach (var id in task.ids) {
                if (!entities.TryGetValue(id, out EntityValue value)) {
                    AddEntityResponseError(id, entities, ref entityErrorInfo);
                    continue;
                }
                var error = value.Error;
                if (error != null) {
                    entityErrorInfo.AddEntityError(error);
                    continue;
                }
                var json = value.Json;  // in case of RemoteClient json is "null"
                var isNull = json.IsNull();
                if (isNull) {
                    // don't remove missing requested peer from EntitySet.peers to preserve info about its absence
                    continue;
                }
                var key     = KeyConvert.IdToKey(id);
                var peer    = set.GetOrCreatePeerByKey(key, id);
                read.result[key] = peer.Entity;
            }
            var keysBuf = set.intern.GetKeysBuf();
            foreach (var findTask in read.findTasks) {
                findTask.SetFindResult(read.result, entities, keysBuf);
            }
            // A ReadTask is set to error if at least one of its JSON results has an error.
            if (entityErrorInfo.HasErrors) {
                read.state.SetError(entityErrorInfo);
                // SetReadTaskError(read, entityErrorInfo); <- must not be called
                return;
            }
            read.state.Executed = true;
            AddReferencesResult(task.references, result.references, read.relations.subRelations);
        }

        private static void SetReadTaskError(ReadTask<TKey, T> read, TaskErrorResult taskError) {
            TaskErrorInfo error = new TaskErrorInfo(taskError);
            read.state.SetError(error);
            SetSubRelationsError(read.relations.subRelations, error);
            foreach (var findTask in read.findTasks) {
                findTask.findState.SetError(error);
            }
        }
        
        private void AddEntityResponseError(in JsonKey id, Dictionary<JsonKey, EntityValue> entities, ref TaskErrorInfo entityErrorInfo) {
            var responseError = new EntityError(EntityErrorType.ReadError, set.name, id, "requested entity missing in response results");
            entityErrorInfo.AddEntityError(responseError);
            var value = new EntityValue(responseError); 
            entities.Add(id, value);
        }
        
        internal override void QueryEntitiesResult(QueryEntities task, SyncTaskResult result, ContainerEntities queryEntities) {
            var query   = (QueryTask<T>)task.syncTask;
            if (result is TaskErrorResult taskError) {
                var taskErrorInfo = new TaskErrorInfo(taskError);
                query.state.SetError(taskErrorInfo);
                SetSubRelationsError(query.relations.subRelations, taskErrorInfo);
                return;
            }
            var queryResult     = (QueryEntitiesResult)result;
            query.resultCursor  = queryResult.cursor;
            var entityErrorInfo = new TaskErrorInfo();
            var entities        = queryEntities.entityMap;
            query.entities      = entities;
            query.ids           = queryResult.ids;
            var results         = query.result = new List<T>(queryResult.ids.Count);
            foreach (var id in queryResult.ids) {
                if (!entities.TryGetValue(id, out EntityValue value)) {
                    AddEntityResponseError(id, entities, ref entityErrorInfo);
                    continue;
                }
                var error = value.Error;
                if (error != null) {
                    entityErrorInfo.AddEntityError(error);
                    continue;
                }
                var key     = KeyConvert.IdToKey(id);
                var peer    = set.GetOrCreatePeerByKey(key, id);
                results.Add(peer.Entity);
            }
            if (entityErrorInfo.HasErrors) {
                query.state.SetError(entityErrorInfo);
                SetSubRelationsError(query.relations.subRelations, entityErrorInfo);
                return;
            }
            AddReferencesResult(task.references, queryResult.references, query.relations.subRelations);
            query.state.Executed = true;
        }

        private void AddReferencesResult(List<References> references, List<ReferencesResult> referencesResult, SubRelations relations) {
            // in case (references != null &&  referencesResult == null) => no reference ids found for references 
            if (references == null || referencesResult == null)
                return;
            for (int n = 0; n < references.Count; n++) {
                References              reference    = references[n];
                ReferencesResult        refResult    = referencesResult[n];
                EntitySet               refContainer = set.intern.store._intern.GetSetByName(reference.container);
                ReadRelationsFunction   subRelation  = relations[reference.selector];
                if (refResult.error != null) {
                    var taskError       = new TaskErrorResult (TaskErrorResultType.DatabaseError, refResult.error);
                    var taskErrorInfo   = new TaskErrorInfo (taskError);
                    subRelation.state.SetError(taskErrorInfo);
                    continue;
                }
                subRelation.SetResult(refContainer, refResult.ids);
                // handle entity errors of subRef task
                var subRefError = subRelation.state.Error;
                if (subRefError.HasErrors) {
                    if (subRefError.TaskError.type != TaskErrorType.EntityErrors)
                        throw new InvalidOperationException("Expect subRef Error.type == EntityErrors");
                    SetSubRelationsError(subRelation.SubRelations, subRefError);
                    continue;
                }
                subRelation.state.Executed = true;
                var subReferences = reference.references;
                if (subReferences != null) {
                    var readRefs = subRelation.SubRelations;
                    AddReferencesResult(subReferences, refResult.references, readRefs);
                }
            }
        }

        private static void SetSubRelationsError(SubRelations relations, TaskErrorInfo taskErrorInfo) {
            foreach (var subRef in relations) {
                subRef.state.SetError(taskErrorInfo);
                SetSubRelationsError(subRef.SubRelations, taskErrorInfo);
            }
        }
        
        internal override void AggregateEntitiesResult (AggregateEntities task, SyncTaskResult result) {
            var aggregate   = (AggregateTask)task.syncTask;
            if (result is TaskErrorResult taskError) {
                // todo set error
                return;
            }
            var aggregateResult         = (AggregateEntitiesResult) result;
            aggregate.result            = aggregateResult.value;
            aggregate.state.Executed    = true;
        }
        
        internal override void CloseCursorsResult (CloseCursors task, SyncTaskResult result) {
            var closeCursor   = (CloseCursorsTask)task.syncTask;
            if (result is TaskErrorResult taskError) {
                // todo set error
                return;
            }
            var closeResult             = (CloseCursorsResult) result;
            closeCursor.count           = closeResult.count; 
            closeCursor.state.Executed  = true;
        }

        /// <summary>
        /// In case of a <see cref="TaskErrorResult"/> add entity errors to <see cref="SyncSet.errorsPatch"/> for all
        /// <see cref="DetectPatchesTask{T}.entityPatches"/> to enable setting <see cref="DetectPatchesTask"/> to
        /// error state via <see cref="DetectPatchesTask{T}.SetResult"/>.
        /// </summary> 
        internal override void PatchEntitiesResult(PatchEntities task, SyncTaskResult result) {
            // task is either a PatchTask<T> or a DetectPatchesTask<T>
            var patchTask       = task.syncTask as PatchTask<T>;
            var detectPatches   = task.syncTask as DetectPatchesTask<T>;
            // ReSharper disable once PossibleNullReferenceException
            var patches         = patchTask != null ? patchTask.entityPatches : detectPatches.entityPatches;
            if (result is TaskErrorResult taskError)
            {
                patchTask?.     state.SetError(new TaskErrorInfo(taskError));
                detectPatches?. state.SetError(new TaskErrorInfo(taskError));
                if (errorsPatch == NoErrors) {
                    errorsPatch = new Dictionary<JsonKey, EntityError>(patches.Count, JsonKey.Equality);
                }
                foreach (var patchPair in patches) {
                    var id = patchPair.Key;
                    var error = new EntityError(EntityErrorType.PatchError, set.name, id, taskError.message){
                        taskErrorType   = taskError.type,
                        stacktrace      = taskError.stacktrace
                    };
                    errorsPatch.TryAdd(id, error);
                }
            } else {
                // var patchResult = (PatchEntitiesResult)result;
                var entityPatches = task.patches;
                foreach (var entityPatch in entityPatches) {
                    var id      = entityPatch.id;
                    var peer    = set.GetPeerById(id);
                    var  nextPatchSource = peer.NextPatchSource;
                    if (nextPatchSource == null)
                        continue;
                    // set patch source (diff reference) only if entity is tracked
                    peer.SetPatchSource(nextPatchSource);
                    peer.SetNextPatchSourceNull();
                }
                patchTask?.SetResult(errorsPatch);
                detectPatches?.SetResult();
            }
            // enable GC to collect references in containers which are not needed anymore
            patches.Clear();
        }

        internal override void DeleteEntitiesResult(DeleteEntities task, SyncTaskResult result) {
            if (task.syncTask is DeleteAllTask<TKey,T> deleteAllTask) {
                deleteAllTask.state.Executed = true;
                return;
            }
            var deleteTask   = (DeleteTask<TKey,T>)task.syncTask;
            if (result is TaskErrorResult taskError) {
                deleteTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var ids = task.ids;
            if (ids != null) {
                foreach (var id in ids) {
                    set.DeletePeer(id);
                }
            }
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
            var subscribeChanges   = (SubscribeChangesTask<T>)task.syncTask;
            if (result is TaskErrorResult taskError) {
                subscribeChanges.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            set.intern.subscription = task.changes.Count > 0 ? task : null;
            subscribeChanges.state.Executed = true;
        }
    }
}
