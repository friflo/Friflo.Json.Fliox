// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

namespace Friflo.Json.Fliox.Hub.Client
{
    // --------------------------------- FlioxClient internals ---------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial class FlioxClient
    {
        private string FormatToString() {
            var sb = new StringBuilder();
            sb.Append('\'');
            sb.Append(DatabaseName);
            sb.Append("'  ");
            ClientInfo.AppendTo(sb);
            return sb.ToString();
        }
        
        private void SetWritePretty (bool value) {
            foreach (var set in _intern.entitySets) {
                set.WritePretty = value;
            }
        }

        private void SetWriteNull (bool value){
            foreach (var set in _intern.entitySets) {
                set.WriteNull = value;
            }
        }
        
        internal void AssertSubscription() {
            if (_intern.eventReceiver == null) {
                var msg = $"The FlioxHub used by the client don't support PushEvents. hub: {_intern.hub.GetType().Name}";
                throw new InvalidOperationException(msg);
            }
        }
        
        internal void ProcessEvents(EventMessage eventMessage) {
            var processor   = _intern.SubscriptionProcessor();
            // Console.WriteLine($"----- ProcessEvent. events: {eventMessages.events.Length}");
            foreach (var ev in eventMessage.events) {
                // Skip already received events
                if (_intern.lastEventSeq >= ev.seq)
                    continue; // could also break as all subsequent events 
            
                _intern.lastEventSeq = ev.seq;                
                processor.ProcessEvent(this, ev);
            }
            if (_intern.ackTimer == null) {
                _intern.ackTimer = new Timer(AcknowledgeEvents);
            }
            if (!_intern.ackTimerPending) {
                _intern.ackTimer.Change(1000, Timeout.Infinite);
                _intern.ackTimerPending = true;
            }
            // if (eventMessage.events.Length > 10) { Console.WriteLine($"--- ProcessEvents {eventMessage.events.Length}"); }
        }
        
        /// <summary> Specific characteristic: Method can run in parallel on any thread </summary>
        private void AcknowledgeEvents(object state) {
            _intern.ackTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _intern.ackTimerPending = false;
            var noAwait = TrySyncAcknowledgeEvents();
            // Console.WriteLine($"--- AcknowledgeEvents");
        } 
        
        private async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            _intern.syncCount++;
            if (_intern.ackTimerPending) {
                _intern.ackTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _intern.ackTimerPending = false;
            }
            Task<ExecuteSyncResult> task = null;
            try {
                task = _intern.hub.ExecuteSync(syncRequest, syncContext);

                _intern.pendingSyncs.TryAdd(task, syncContext);
                var response = await task.ConfigureAwait(false);
                
                // The Hub returns a client id if the client didn't provide one and one of its task require one. 
                var success = response.success;
                if (_intern.clientId.IsNull() && success != null && !success.clientId.IsNull()) {
                    SetClientId(success.clientId);
                }
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
            int count = _intern.subscriptions?.Count ?? 0;
            foreach (var set in _intern.entitySets) {
                if (set.GetSubscription() != null)
                    count++;
            }
            return count;
        }
        
        private List<SyncTask> GetTasks() {
            var functions = _intern.syncStore.functions;
            var result = new List<SyncTask>(functions.Count);
            foreach (var function in functions) {
                if (function is SyncTask syncTask)
                    result.Add(syncTask);
            }
            return result;
        }
        
        internal void AddTask(SyncTask task) {
            _intern.syncStore.functions.Add(task);
        }
        
        internal void AddFunction(SyncFunction task) {
            _intern.syncStore.functions.Add(task);
        }
        
        internal EntitySet GetEntitySet(string name) {
            if (_intern.TryGetSetByName(name, out var entitySet))
                return entitySet;
            throw new InvalidOperationException($"unknown EntitySet. name: {name}");
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
        /// <see cref="SyncRequest.eventAck"/> is set to acknowledge already received events to clear
        /// <see cref="EventSubClient.sentEventsQueue"/>. This avoids resending already received events on reconnect. 
        /// </summary>
        private SyncRequest CreateSyncRequest(out SyncStore syncStore, ObjectMapper mapper) {
            syncStore = _intern.syncStore;
            _intern.syncStore = new SyncStore();
            
            syncStore.SetSyncSets(this);
            
            var functions   = syncStore.functions;
            var tasks       = new List<SyncRequestTask>(functions.Count);
            var context = new CreateTaskContext (mapper);
            foreach (var function in functions) {
                if (function is SyncTask task) {
                    var requestTask = task.CreateRequestTask(context);
                    tasks.Add(requestTask);
                }
            }
            // --- create new SyncStore and SyncSet's to collect future SyncTask's and execute them via the next SyncTasks() call 
            foreach (var set in _intern.entitySets) {
                set.ResetSync();
            }
            return CreateSyncRequestInstance(tasks);
        }
        
        private SyncRequest CreateSyncRequestInstance(List<SyncRequestTask> tasks) {
            return new SyncRequest {
                database    = _intern.database,
                tasks       = tasks,
                userId      = _intern.userId,
                clientId    = _intern.clientId, 
                token       = _intern.token,
                eventAck    = _intern.lastEventSeq
            };
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

        private static void CopyEntityErrors(List<SyncRequestTask> tasks, List<SyncTaskResult> responseTasks, SyncStore syncStore) {
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
        /// These properties are set by <see cref="RemoteHost.SetContainerResults"/>.
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
                    ProcessSyncTasks(syncRequest, response, syncStore, pooled.instance);
                    syncStore.DetectPatchesResults();
                }
                finally {
                    var functions   = syncStore.functions;
                    var failed      = GetFailedFunctions(functions);
                    syncResult      = new SyncResult(functions, failed, response.error);
                    
                    foreach (var function in functions) {
                        var onSync  = function.OnSync;
                        if (onSync == null)
                            continue;
                        var taskError = function.State.Error.TaskError;
                        try {
                            onSync(taskError);
                        }
                        catch (Exception e) {
                            var error = $"OnSync Exception in {function.GetLabel()}";
                            Logger.Log(HubLog.Error, error, e);
                        }
                    }
                }
                return syncResult;
            }
        }
        
        private static IReadOnlyList<SyncFunction> GetFailedFunctions(List<SyncFunction> functions) {
            // create failed array only if required
            var errorCount  = 0;
            foreach (var task in functions) {
                if (!task.State.Error.HasErrors)
                    continue;
                errorCount++;
            }
            if (errorCount == 0) {
                return Array.Empty<SyncFunction>();
            }
            var failed = new SyncFunction[errorCount];
            int n = 0;
            foreach (var task in functions) {
                if (!task.State.Error.HasErrors)
                    continue;
                failed[n++] = task;
            }
            return failed;
        }

        private void ProcessSyncTasks(SyncRequest syncRequest, ExecuteSyncResult response, SyncStore syncStore, ObjectMapper mapper)
        {
            var             tasks           = syncRequest.tasks;
            ErrorResponse   error           = response.error;
            
            if (error != null) {
                // ----------- handle ErrorResponse -----------
                var syncError       = new TaskErrorResult (TaskErrorResultType.SyncError, error.message);
                var emptyResults    = new Dictionary<string, ContainerEntities>();
                // process all task using by passing an error 
                for (int n = 0; n < tasks.Count; n++) {
                    SyncRequestTask task    = tasks[n];
                    ProcessTaskResult(task, syncError, syncStore, emptyResults, mapper);
                }
                return;
            }
            // ----------- handle SyncResponse -----------
            response.success.AssertResponse(syncRequest);
            SyncResponse    syncResponse    = response.success;
            var             hub             = _intern.hub; 
            if (hub is RemoteClientHub) {
                GetContainerResults(syncResponse);
            }
            var containerResults = syncResponse.resultMap;
            foreach (var containerResult in containerResults) {
                ContainerEntities containerEntities = containerResult.Value;
                EntitySet set = _intern.GetSetByName(containerResult.Key);
                set.SyncPeerEntityMap(containerEntities.entityMap, mapper);
            }
            var responseTasks = syncResponse.tasks;
            // Ensure every response task result type matches its task
            for (int n = 0; n < tasks.Count; n++) {
                var task        = tasks[n];
                var taskType    = task.TaskType;
                var result      = responseTasks[n];
                var actual      = result.TaskType;
                if (actual == TaskType.error)
                    continue;
                if (taskType == actual)
                    continue;
                var msg = $"Expect task type of response matches request. index:{n} expect: {taskType} actual: {actual}";
                throw new InvalidOperationException(msg);
            }
            CopyEntityErrors(tasks, responseTasks, syncStore);
            
            // process all tasks by passing the related response task result
            for (int n = 0; n < tasks.Count; n++) {
                SyncRequestTask task    = tasks[n];
                SyncTaskResult  result  = responseTasks[n];
                ProcessTaskResult(task, result, syncStore, containerResults, mapper);
            }
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
                    SyncStore.MessageResult(message, result);
                    break;
                case TaskType.command:
                    var command =           (SendCommand)       task;
                    SyncStore.MessageResult(command, result);
                    break;
                case TaskType.subscribeChanges:
                    var subscribeChanges =  (SubscribeChanges)  task;
                    syncSet = syncSets[subscribeChanges.container];
                    syncSet.SubscribeChangesResult(subscribeChanges, result);
                    break;
                case TaskType.subscribeMessage:
                    var subscribeMessage =  (SubscribeMessage)  task;
                    SyncStore.SubscribeMessageResult(subscribeMessage, result);
                    break;
            }
        }
        
        private EntitySet[] GetEntitySets() {
            var entitySets  = new EntitySet[_intern.entitySets.Length];
            int n           = 0;
            foreach (var entitySet in _intern.entitySets) {
                entitySets[n++] = entitySet;
            }
            return entitySets;
        }
    }
}