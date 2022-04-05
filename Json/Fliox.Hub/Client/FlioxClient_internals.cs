// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client
{
    // --------------------------------- FlioxClient internals ---------------------------------
    public partial class FlioxClient
    {
        internal void AssertSubscriptionProcessor() {
            if (_intern.subscriptionProcessor != null)
                return;
            var msg = $"subscriptions require a {nameof(SubscriptionProcessor)} - {nameof(SetSubscriptionProcessor)}() before";
            throw new InvalidOperationException(msg);
        }
        
        private async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, ExecuteContext executeContext) {
            _intern.syncCount++;
            Task<ExecuteSyncResult> task = null;
            try {
                task = _intern.hub.ExecuteSync(syncRequest, executeContext);

                _intern.pendingSyncs.TryAdd(task, executeContext);
                var response = await task.ConfigureAwait(false);
                _intern.pendingSyncs.TryRemove(task, out _);
                return response;
            }
            catch (Exception e) {
                _intern.pendingSyncs.TryRemove(task, out _);
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return new ExecuteSyncResult(errorMsg, ErrorResponseType.Exception);
            }
        }
        
        // ReSharper disable once UnusedMember.Local
        private int GetSubscriptionCount() {
            int count = _intern.subscriptions.Count;
            foreach (var setPair in _intern.setByType) {
                var set = setPair.Value;
                if (set.GetSubscription() != null)
                    count++;
            }
            return count;
        }
        
        internal void AddTask(SyncTask task) {
            _intern.syncStore.appTasks.Add(task);
        }
        
        internal EntitySet GetEntitySet(string name) {
            if (_intern.TryGetSetByName(name, out var entitySet))
                return entitySet;
            throw new InvalidOperationException($"unknown EntitySet. name: {name}");
        }
        
        // TAG_NULL_REF
        internal EntitySetBase<T> GetEntitySetBase<T>() where T : class {
            Type entityType = typeof(T);
            if (_intern.TryGetSetByType(entityType, out EntitySet set))
                return (EntitySetBase<T>)set;
            throw new InvalidOperationException($"unknown EntitySet<{entityType.Name}>");
        }

        internal EntitySet<TKey, T> GetEntitySet<TKey, T>() where T : class {
            Type entityType = typeof(T);
            if (_intern.TryGetSetByType(entityType, out EntitySet set))
                return (EntitySet<TKey, T>)set;
            throw new InvalidOperationException($"unknown EntitySet<{entityType.Name}>");
        }
        
        private SyncRequest CreateSyncRequest(out SyncStore syncStore) {
            using (var pooled = ObjectMapper.Get()) {
                return CreateSyncRequest(out syncStore, pooled.instance);
            }          
        }

        /// <summary>
        /// Returning current <see cref="ClientIntern.syncStore"/> as <paramref name="syncStore"/> enables request handling
        /// in a worker thread while calling <see cref="SyncStore"/> methods from 'main' thread.
        /// 
        /// If store has <see cref="ClientIntern.subscriptionProcessor"/> acknowledge received events to clear
        /// <see cref="Host.Event.EventSubscriber.sentEvents"/>. This avoids resending already received events on reconnect. 
        /// </summary>
        private SyncRequest CreateSyncRequest(out SyncStore syncStore, ObjectMapper mapper) {
            mapper.TracerContext = _intern.tracerContext;
            syncStore = _intern.syncStore;
            syncStore.SetSyncSets(this);
            
            var tasks       = new List<SyncRequestTask>();
            var syncRequest = new SyncRequest {
                database    = _intern.database,
                tasks       = tasks,
                userId      = _intern.userId,
                clientId    = _intern.clientId, 
                token       = _intern.token
            };

            // see method docs
            if (_intern.subscriptionProcessor != null) {
                syncRequest.eventAck = _intern.lastEventSeq;
            }

            foreach (var setPair in _intern.setByType) {
                EntitySet set       = setPair.Value;
                var setInfo         = set.SetInfo;
                var curTaskCount    = tasks.Count;
                var syncSet         = set.SyncSet;
                // ReSharper disable once UseNullPropagation
                if (syncSet != null) {
                    syncSet.AddTasks(tasks, mapper);
                }
                AssertTaskCount(setInfo, tasks.Count - curTaskCount);
            }
            syncStore.AddTasks(tasks);
            
            // --- create new SyncStore and SyncSet's to collect future SyncTask's and execute them via the next SyncTasks() call 
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.ResetSync();
            }
            _intern.syncStore = new SyncStore();
            return syncRequest;
        }

        [Conditional("DEBUG")]
        private static void AssertTaskCount(in SetInfo setInfo, int taskCount) {
            int expect  = setInfo.tasks; 
            if (expect != taskCount)
                throw new InvalidOperationException($"Unexpected task.Count. expect: {expect}, got: {taskCount}");
        }
        
        private static void CopyEntityErrorsToMap(List<EntityError> errors, string container, ref IDictionary<JsonKey, EntityError> errorMap) {
            foreach (var error in errors) {
                // error .container is not serialized as it is redundant data.
                // Infer its value from containing error List
                error.container = container;
            }
            if (errorMap == SyncSet.NoErrors) {
                errorMap = new Dictionary<JsonKey, EntityError>(errors.Count, JsonKey.Equality);
            }
            foreach (var error in errors) {
                errorMap.Add(error.id, error);
            }
        }

        private static void SetErrors(List<SyncRequestTask> tasks, List<SyncTaskResult> responseTasks, SyncStore syncStore) {
            var syncSets = syncStore.SyncSets;
            
            for (int n = 0; n < tasks.Count; n++) {
                var task            = tasks[n];
                var responseTask    = responseTasks[n];
                switch (responseTask.TaskType) {
                    case TaskType.upsert:
                        var upsertResult    = (UpsertEntitiesResult)responseTask;
                        if (upsertResult.errors == null)
                            continue;
                        var container       = ((UpsertEntities)task).container;
                        SyncSet syncSet     = syncSets[container];
                        CopyEntityErrorsToMap(upsertResult.errors,  container, ref syncSet.errorsUpsert);
                        break; 
                    case TaskType.create:
                        var createResult    = (CreateEntitiesResult)responseTask;
                        if (createResult.errors == null)
                            continue;
                        container           = ((CreateEntities)task).container;
                        syncSet             = syncSets[container];
                        CopyEntityErrorsToMap(createResult.errors,  container, ref syncSet.errorsCreate);
                        break;
                    case TaskType.patch:
                        var patchResult     = (PatchEntitiesResult)responseTask;
                        if (patchResult.errors == null)
                            continue;
                        container           = ((PatchEntities)task).container;
                        syncSet             = syncSets[container];
                        CopyEntityErrorsToMap(patchResult.errors,   container, ref syncSet.errorsPatch);
                        break;
                    case TaskType.delete:
                        var deleteResult    = (DeleteEntitiesResult)responseTask;
                        if (deleteResult.errors == null)
                            continue;
                        container           = ((DeleteEntities)task).container;
                        syncSet             = syncSets[container];
                        CopyEntityErrorsToMap(deleteResult.errors,  container, ref syncSet.errorsDelete);
                        break;
                }
            }
        }
        
        /// Map <see cref="ContainerEntities.entities"/>, <see cref="ContainerEntities.notFound"/> and
        /// <see cref="ContainerEntities.errors"/> to <see cref="ContainerEntities.entityMap"/>.
        /// These properties are set by <see cref="RemoteHostHub.SetContainerResults"/>.
        private void GetContainerResults(SyncResponse response) {
            var containers      = response.containers;
            response.containers = null;
            if (containers == null) {
                response.resultMap = new Dictionary<string, ContainerEntities>();
                return;
            }
            var resultMap       = new Dictionary<string, ContainerEntities>(containers.Count);
            response.resultMap  = resultMap;
            foreach (var result in containers) {
                resultMap.Add(result.container, result);
            }
            var processor = _intern.EntityProcessor();
            foreach (var container in containers) {
                string name         = container.container;
                if (!_intern.TryGetSetByName(name, out EntitySet set)) {
                    continue;
                }
                var keyName         = set.GetKeyName();
                var entityMap       = container.entityMap;
                var entities        = container.entities;
                var notFound        = container.notFound;
                var notFoundCount   = notFound?.Count ?? 0;
                var errors          = container.errors;
                var errorCount      = errors?.Count ?? 0;
                container.errors    = null;
                entityMap.Clear(); // Not necessary, be safe
                entityMap.EnsureCapacity(entities.Count + notFoundCount + errorCount);
                
                // --- entities
                foreach (var entity in entities) {
                    if (!processor.GetEntityKey(entity, keyName, out JsonKey key, out string errorMsg)) {
                        throw new InvalidOperationException($"GetEntityResults not found: {errorMsg}");
                    }
                    entityMap.Add(key, new EntityValue(entity));
                }
                entities.Clear();
                container.entities = null;
                
                // --- notFound
                if (notFound != null) {
                    foreach (var notFoundKey in notFound) {
                        entityMap.Add(notFoundKey, new EntityValue());
                    }
                    notFound.Clear();
                    container.notFound = null;
                }
                
                // --- errors
                if (errors == null || errors.Count == 0)
                    continue;
                foreach (var error in errors) {
                    entityMap.Add(error.id, new EntityValue(error));
                }
                errors.Clear();
                container.errors = null;
            }
            containers.Clear();
        }
        
        private SyncResult HandleSyncResponse(SyncRequest syncRequest, ExecuteSyncResult response, SyncStore syncStore) {
            using (var pooled = ObjectMapper.Get()) {
                SyncResult syncResult;
                try {
                    HandleSyncResponse(syncRequest, response, syncStore, pooled.instance);
                }
                finally {
                    var failed = new List<SyncTask>();
                    foreach (SyncTask task in syncStore.appTasks) {
                        task.AddFailedTask(failed);
                    }
                    syncResult = new SyncResult(syncStore.appTasks, failed, response.error);
                }
                return syncResult;
            }
        }

        private void HandleSyncResponse(SyncRequest syncRequest, ExecuteSyncResult response, SyncStore syncStore, ObjectMapper mapper) {
            mapper.TracerContext = _intern.tracerContext;
            
            ErrorResponse   error       = response.error;
            TaskErrorResult syncError;
            Dictionary<string, ContainerEntities>   containerResults;
            
            if (error == null) {
                var result = response.success;
                response.success.AssertResponse(syncRequest);
                syncError = null;
                var hub = _intern.hub; 
                if (hub is RemoteClientHub)
                    GetContainerResults(result);
                containerResults = result.resultMap;
                foreach (var containerResult in containerResults) {
                    ContainerEntities containerEntities = containerResult.Value;
                    var set = _intern.GetSetByName(containerResult.Key);
                    set.SyncPeerEntities(containerEntities.entityMap, mapper);
                }
                SetErrors(syncRequest.tasks, result.tasks, syncStore);
            } else {
                syncError = new TaskErrorResult (TaskErrorResultType.SyncError, error.message);
                containerResults = new Dictionary<string, ContainerEntities>();
            }

            var tasks = syncRequest.tasks;
            for (int n = 0; n < tasks.Count; n++) {
                var task = tasks[n];
                TaskType    taskType = task.TaskType;
                SyncTaskResult  result;
                if (syncError == null) {
                    var results = response.success.tasks;
                    result = results[n];
                    var actual = result.TaskType;
                    if (actual != TaskType.error) {
                        if (taskType != actual) {
                            var msg = $"Expect task type of response matches request. index:{n} expect: {taskType} actual: {actual}";
                            throw new InvalidOperationException(msg);
                        }
                    }
                } else {
                    result = syncError;
                }
                ProcessTaskResult(task, result, syncStore, containerResults, mapper);
            }
            syncStore.LogResults();
        }
        
        private static void ProcessTaskResult (
            SyncRequestTask                         task,
            SyncTaskResult                          result,
            SyncStore                               syncStore,
            Dictionary<string, ContainerEntities>   containerResults,
            ObjectMapper                            mapper)
        {
            var syncSets    = syncStore.SyncSets;
            switch (task.TaskType) {
                case TaskType.reserveKeys:
                    var reserveKeys =       (ReserveKeys)       task;
                    var syncSet = syncSets[reserveKeys.container];
                    syncSet.ReserveKeysResult(reserveKeys, result);
                    break;
                case TaskType.create:
                    var create =            (CreateEntities)    task;
                    syncSet = syncSets[create.container];
                    syncSet.CreateEntitiesResult(create, result, mapper);
                    break;
                case TaskType.upsert:
                    var upsert =            (UpsertEntities)    task;
                    syncSet = syncSets[upsert.container];
                    syncSet.UpsertEntitiesResult(upsert, result, mapper);
                    break;
                case TaskType.read:
                    var readList =          (ReadEntities)      task;
                    syncSet = syncSets[readList.container];
                    containerResults.TryGetValue(readList.container, out ContainerEntities entities);
                    syncSet.ReadEntitiesResult(readList, result, entities);
                    break;
                case TaskType.query:
                    var query =             (QueryEntities)     task;
                    syncSet = syncSets[query.container];
                    containerResults.TryGetValue(query.container, out ContainerEntities queryEntities);
                    syncSet.QueryEntitiesResult(query, result, queryEntities);
                    break;
                case TaskType.closeCursors:
                    var closeCursors =      (CloseCursors)      task;
                    syncSet = syncSets[closeCursors.container];
                    syncSet.CloseCursorsResult(closeCursors, result);
                    break;
                case TaskType.aggregate:
                    var aggregate =         (AggregateEntities) task;
                    syncSet = syncSets[aggregate.container];
                    syncSet.AggregateEntitiesResult(aggregate, result);
                    break;
                case TaskType.patch:
                    var patch =             (PatchEntities)     task;
                    syncSet = syncSets[patch.container];
                    syncSet.PatchEntitiesResult(patch, result);
                    break;
                case TaskType.delete:
                    var delete =            (DeleteEntities)    task;
                    syncSet = syncSets[delete.container];
                    syncSet.DeleteEntitiesResult(delete, result);
                    break;
                case TaskType.message:
                    var message =           (SendMessage)       task;
                    syncStore.MessageResult(message, result);
                    break;
                case TaskType.command:
                    var command =           (SendCommand)       task;
                    syncStore.MessageResult(command, result);
                    break;
                case TaskType.subscribeChanges:
                    var subscribeChanges =  (SubscribeChanges)  task;
                    syncSet = syncSets[subscribeChanges.container];
                    syncSet.SubscribeChangesResult(subscribeChanges, result);
                    break;
                case TaskType.subscribeMessage:
                    var subscribeMessage =  (SubscribeMessage)  task;
                    syncStore.SubscribeMessageResult(subscribeMessage, result);
                    break;
            }
        }
    }
}