// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    internal abstract class SyncSet
    {
        internal static readonly IDictionary<JsonKey, EntityError> NoErrors = new EmptyDictionary<JsonKey, EntityError>();  
            
        internal    IDictionary<JsonKey, EntityError>    errorsCreate = NoErrors;
        internal    IDictionary<JsonKey, EntityError>    errorsUpsert = NoErrors;
        internal    IDictionary<JsonKey, EntityError>    errorsPatch  = NoErrors;
        internal    IDictionary<JsonKey, EntityError>    errorsDelete = NoErrors;

        internal  abstract  void    AddTasks                (List<SyncRequestTask> tasks);
        
        internal  abstract  void    ReserveKeysResult       (ReserveKeys        task, SyncTaskResult result);
        internal  abstract  void    CreateEntitiesResult    (CreateEntities     task, SyncTaskResult result);
        internal  abstract  void    UpsertEntitiesResult    (UpsertEntities     task, SyncTaskResult result);
        internal  abstract  void    ReadEntitiesListResult  (ReadEntitiesList   task, SyncTaskResult result, ContainerEntities readEntities);
        internal  abstract  void    QueryEntitiesResult     (QueryEntities      task, SyncTaskResult result, ContainerEntities queryEntities);
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
    }

    internal partial class SyncSet<TKey, T>
    {
        internal override void ReserveKeysResult (ReserveKeys task, SyncTaskResult result) {
            var reserve = _reserveKeys;
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
            reserve.count      = count;
            reserve.keys       = keys;
            reserve.token      = resultKeys.token;
            reserve.state.Synced = true;
        }
        
        /// In case of a <see cref="TaskErrorResult"/> add entity errors to <see cref="SyncSet.errorsCreate"/> for all
        /// <see cref="Creates"/> to enable setting <see cref="LogTask"/> to error state via <see cref="LogTask.SetResult"/>. 
        internal override void CreateEntitiesResult(CreateEntities task, SyncTaskResult result) {
            CreateUpsertEntitiesResult(task.entityKeys, task.entities, result, CreateTasks(), errorsCreate);
            var creates = Creates();
            if (result is TaskErrorResult taskError) {
                if (errorsCreate == NoErrors) {
                    errorsCreate = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
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
            // enable GC to collect references in containers which are not needed anymore
            creates.Clear();
            CreateTasks().Clear();
        }

        internal override void UpsertEntitiesResult(UpsertEntities task, SyncTaskResult result) {
            CreateUpsertEntitiesResult(task.entityKeys, task.entities, result, UpsertTasks(), errorsUpsert);
            
            // enable GC to collect references in containers which are not needed anymore
            Upserts().Clear();
            UpsertTasks().Clear();
        }

        private void CreateUpsertEntitiesResult(
            List<JsonKey>                       keys,
            List<JsonValue>                     entities,
            SyncTaskResult                          result,
            List<WriteTask>                     writeTasks,
            IDictionary<JsonKey, EntityError>   writeErrors)
        {
            if (result is TaskErrorResult taskError) {
                foreach (var writeTask in writeTasks) {
                    writeTask.state.SetError(new TaskErrorInfo(taskError));
                }
                return;
            }
            if (keys.Count != entities.Count)
                throw new InvalidOperationException("Expect equal counts");
            var reader = set.intern.jsonMapper.reader;
            for (int n = 0; n < entities.Count; n++) {
                var entity = entities[n];
                // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                var id = keys[n];
                if (writeErrors.TryGetValue(id, out EntityError _)) {
                    continue;
                }
                var key = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                var peer = set.GetOrCreatePeerByKey(key, id);
                peer.created = false;
                peer.updated = false;
                peer.SetPatchSource(reader.Read<T>(entity.json));
            }
            foreach (var writeTask in writeTasks) {
                var entityErrorInfo = new TaskErrorInfo();
                var idsBuf = set.intern.store._intern.idsBuf;
                idsBuf.Clear();
                writeTask.GetIds(idsBuf);
                foreach (var id in idsBuf) {
                    if (writeErrors.TryGetValue(id, out EntityError error)) {
                        entityErrorInfo.AddEntityError(error);
                    }
                }
                if (entityErrorInfo.HasErrors) {
                    writeTask.state.SetError(entityErrorInfo);
                    continue;
                }
                writeTask.state.Synced = true;
            }
        }

        internal override void ReadEntitiesListResult(ReadEntitiesList taskList, SyncTaskResult result, ContainerEntities readEntities) {
            var reads = Reads();
            if (result is TaskErrorResult taskError) {
                foreach (var read in reads) {
                    SetReadTaskError(read, taskError);
                }
                return;
            }
            var readListResult = (ReadEntitiesListResult) result;
            var expect = reads.Count;
            var actual = taskList.reads.Count;
            if (expect != actual) {
                throw new InvalidOperationException($"Expect reads.Count == result.reads.Count. expect: {expect}, actual: {actual}");
            }

            for (int i = 0; i < taskList.reads.Count; i++) {
                var task = taskList.reads[i];
                var read = reads[i];
                var readResult = readListResult.reads[i];
                ReadEntitiesResult(task, readResult, read, readEntities);
            }
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
                    // dont remove missing requested peer from EntitySet.peers to preserve info about its absence
                    continue;
                }
                var key = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                var peer = set.GetOrCreatePeerByKey(key, id);
                read.results[key] = peer.Entity;
            }
            foreach (var findTask in read.findTasks) {
                findTask.SetFindResult(read.results, entities, set.intern.keysBuf);
            }
            // A ReadTask is set to error if at least one of its JSON results has an error.
            if (entityErrorInfo.HasErrors) {
                read.state.SetError(entityErrorInfo);
                // SetReadTaskError(read, entityErrorInfo); <- must not be called
                return;
            }
            read.state.Synced = true;
            AddReferencesResult(task.references, result.references, read.refsTask.subRefs);
        }

        private static void SetReadTaskError(ReadTask<TKey, T> read, TaskErrorResult taskError) {
            TaskErrorInfo error = new TaskErrorInfo(taskError);
            read.state.SetError(error);
            SetSubRefsError(read.refsTask.subRefs, error);
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
            var filterLinq = task.filterLinq;
            var query = Queries()[filterLinq];
            if (result is TaskErrorResult taskError) {
                var taskErrorInfo = new TaskErrorInfo(taskError);
                query.state.SetError(taskErrorInfo);
                SetSubRefsError(query.refsTask.subRefs, taskErrorInfo);
                return;
            }
            var queryResult     = (QueryEntitiesResult)result;
            var entityErrorInfo = new TaskErrorInfo();
            var entities        = queryEntities.entityMap;
            var results         = query.results = new Dictionary<TKey, T>(queryResult.ids.Count);
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
                var key = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                var peer = set.GetOrCreatePeerByKey(key, id);
                results.Add(key, peer.Entity);
            }
            if (entityErrorInfo.HasErrors) {
                query.state.SetError(entityErrorInfo);
                SetSubRefsError(query.refsTask.subRefs, entityErrorInfo);
                return;
            }
            AddReferencesResult(task.references, queryResult.references, query.refsTask.subRefs);
            query.state.Synced = true;
        }

        private void AddReferencesResult(List<References> references, List<ReferencesResult> referencesResult, SubRefs refs) {
            // in case (references != null &&  referencesResult == null) => no reference ids found for references 
            if (references == null || referencesResult == null)
                return;
            for (int n = 0; n < references.Count; n++) {
                References          reference    = references[n];
                ReferencesResult    refResult    = referencesResult[n];
                EntitySet           refContainer = set.intern.store._intern.setByName[reference.container];
                ReadRefsTask        subRef       = refs[reference.selector];
                if (refResult.error != null) {
                    var taskErrorInfo = new TaskErrorInfo(new TaskErrorResult {
                        type    = TaskErrorResultType.DatabaseError,
                        message = refResult.error 
                    });
                    subRef.state.SetError(taskErrorInfo);
                    continue;
                }
                subRef.SetResult(refContainer, refResult.ids);
                // handle entity errors of subRef task
                var subRefError = subRef.state.Error;
                if (subRefError.HasErrors) {
                    if (subRefError.TaskError.type != TaskErrorType.EntityErrors)
                        throw new InvalidOperationException("Expect subRef Error.type == EntityErrors");
                    SetSubRefsError(subRef.SubRefs, subRefError);
                    continue;
                }
                subRef.state.Synced = true;
                var subReferences = reference.references;
                if (subReferences != null) {
                    var readRefs = subRef.SubRefs;
                    AddReferencesResult(subReferences, refResult.references, readRefs);
                }
            }
        }

        private static void SetSubRefsError(SubRefs refs, TaskErrorInfo taskErrorInfo) {
            foreach (var subRef in refs) {
                subRef.state.SetError(taskErrorInfo);
                SetSubRefsError(subRef.SubRefs, taskErrorInfo);
            }
        }

        /// In case of a <see cref="TaskErrorResult"/> add entity errors to <see cref="SyncSet.errorsPatch"/> for all
        /// <see cref="Patches"/> to enable setting <see cref="LogTask"/> to error state via <see cref="LogTask.SetResult"/>. 
        internal override void PatchEntitiesResult(PatchEntities task, SyncTaskResult result) {
            var patchTasks  = PatchTasks();
            var patches     = Patches();
            if (result is TaskErrorResult taskError) {
                foreach (var patchTask in patchTasks) {
                    patchTask.state.SetError(new TaskErrorInfo(taskError));
                }
                if (errorsPatch == NoErrors) {
                    errorsPatch = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
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
                foreach (var entityPatchPair in entityPatches) {
                    var id = entityPatchPair.Key;
                    var peer = set.GetPeerById(id);
                    peer.SetPatchSource(peer.NextPatchSource);
                    peer.SetNextPatchSourceNull();
                }
                foreach (var patchTask in patchTasks) {
                    var entityErrorInfo = new TaskErrorInfo();
                    foreach (var peer in patchTask.peers) {
                        if (errorsPatch.TryGetValue(peer.id, out EntityError error)) {
                            entityErrorInfo.AddEntityError(error);
                        }
                    }
                    if (entityErrorInfo.HasErrors) {
                        patchTask.state.SetError(entityErrorInfo);
                    } else {
                        patchTask.state.Synced = true;
                    }
                }
            }
            // enable GC to collect references in containers which are not needed anymore
            patches.Clear();
            patchTasks.Clear();
        }

        internal override void DeleteEntitiesResult(DeleteEntities task, SyncTaskResult result) {
            if (result is TaskErrorResult taskError) {
                foreach (var deleteTask in DeleteTasks()) {
                    deleteTask.state.SetError(new TaskErrorInfo(taskError));
                }
                return;
            }
            foreach (var id in task.ids) {
                set.DeletePeer(id);
            }
            foreach (var deleteTask in DeleteTasks()) {
                var entityErrorInfo = new TaskErrorInfo();
                var keysBuf = set.intern.keysBuf;
                keysBuf.Clear();
                deleteTask.GetKeys(keysBuf);
                foreach (var key in keysBuf) {
                    var id = Ref<TKey,T>.RefKeyMap.KeyToId(key);
                    if (errorsDelete.TryGetValue(id, out EntityError error)) {
                        entityErrorInfo.AddEntityError(error);
                    }
                }
                if (entityErrorInfo.HasErrors) {
                    deleteTask.state.SetError(entityErrorInfo);
                    continue;
                }
                deleteTask.state.Synced = true;
            }
            if (_deleteTaskAll != null) {
                _deleteTaskAll.state.Synced = true;
            }
        }
        
        internal override void SubscribeChangesResult (SubscribeChanges task, SyncTaskResult result) {
            if (result is TaskErrorResult taskError) {
                subscribeChanges.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            set.intern.subscription = task.changes.Count > 0 ? task : null;
            subscribeChanges.state.Synced = true;
        }
    }
}
