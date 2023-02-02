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
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

// ReSharper disable once CheckNamespace
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
        
        /// <summary>
        /// Process the <see cref="EventMessage.events"/> of the passed serialized <see cref="EventMessage"/>
        /// </summary>
        /// <remarks>
        /// This method is not reentrant.
        /// The calling <see cref="EventProcessor"/> ensures this method is called sequentially.
        /// </remarks>
        internal void ProcessEvents(in JsonValue rawEventMessage) {
            var mapper          = _intern.ObjectMapper();
            var reader          = mapper.reader;
            reader.ReaderPool   = _intern.EventReaderPool().Reuse();
            var eventMessage    = reader.Read<EventMessage>(rawEventMessage);
            if (reader.Error.ErrSet) {
                var error = reader.Error.msg.AsString();
                Logger.Log(HubLog.Error, error);
                return;
            }
            // Skip already received events
            if (_intern.lastEventSeq < eventMessage.seq) {
                _intern.lastEventSeq = eventMessage.seq;   
                var processor = _intern.SubscriptionProcessor();
                // Console.WriteLine($"----- ProcessEvent. events: {eventMessages.events.Length}");
                foreach (var syncEvent in eventMessage.events) {
                    processor.ProcessEvent(this, syncEvent, eventMessage.seq);
                }
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
            
            TrySyncAcknowledgeEvents().ContinueWith(task => {
                Logger.Log(HubLog.Error, "AcknowledgeEvents() error", task.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
            // Console.WriteLine($"--- AcknowledgeEvents");
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
        
        internal EntitySet GetEntitySet(in ShortString name) {
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
        /// By default a new <see cref="MemoryBuffer"/> is created as its array may be used by the application
        /// at any time later. <br/>
        /// It returns a pooled <see cref="MemoryBuffer"/> only if the application calls <see cref="SyncResult.Reuse"/>
        /// indicating its safe to do so.
        /// </summary>
        private MemoryBuffer CreateMemoryBuffer() {
            return _intern.memoryBufferPool.Get() ?? new MemoryBuffer(MemoryBufferCapacity);
        }
        
        private SyncContext CreateSyncContext(MemoryBuffer memoryBuffer) {
            var syncContext = _intern.syncContextBuffer.Get() ?? new SyncContext(_intern.sharedEnv, _intern.eventReceiver); 
            syncContext.SetMemoryBuffer(memoryBuffer);
            syncContext.clientId            = _intern.clientId;
            syncContext.responseReaderPool  = _intern.responseReaderPool?.Get().instance.Reuse();
            return syncContext;
        }
        
        private void ReuseSyncContext(SyncContext syncContext) {
            _intern.responseReaderPool?.Return(syncContext.responseReaderPool);
            _intern.syncContextBuffer.Add(syncContext);
            syncContext.Init();
        }

        /// <summary>
        /// Returning current <see cref="ClientIntern.syncStore"/> as <paramref name="syncStore"/> enables request handling
        /// in a worker thread while calling <see cref="SyncStore"/> methods from 'main' thread.
        /// 
        /// <see cref="SyncRequest.eventAck"/> is set to acknowledge already received events to clear
        /// <see cref="EventSubClient.sentEventMessages"/>. This avoids resending already received events on reconnect. 
        /// </summary>
        private SyncRequest CreateSyncRequest(out SyncStore syncStore, ObjectMapper mapper) {
            syncStore           = _intern.syncStore;
            _intern.syncStore   = _intern.syncStoreBuffer.Get() ?? new SyncStore();
            
            syncStore.SetSyncSets(this);
            
            var functions       = syncStore.functions;
            var syncRequest     = _intern.syncRequestBuffer.Get() ?? new SyncRequest();
            InitSyncRequest(syncRequest);
            var tasks           = syncRequest.tasks ?? new List<SyncRequestTask>(functions.Count);
            syncRequest.tasks   = tasks;
            var context         = new CreateTaskContext (mapper);
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
            return syncRequest;
        }
        
        private void InitSyncRequest(SyncRequest syncRequest) {
            syncRequest.database    = _intern.databaseShort;
            syncRequest.userId      = _intern.userId;
            syncRequest.clientId    = _intern.clientId; 
            syncRequest.token       = _intern.token;
            syncRequest.eventAck    = _intern.lastEventSeq;
        }

        private static void CopyEntityErrorsToMap(List<EntityError> errors, in ShortString container, ref IDictionary<JsonKey, EntityError> errorMap) {
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

        private static readonly IDictionary<ShortString, SyncSet> EmptySynSet = new EmptyDictionary<ShortString, SyncSet>();

        private static void CopyEntityErrors(List<SyncRequestTask> tasks, List<SyncTaskResult> responseTasks, SyncStore syncStore) {
            var syncSets = syncStore.SyncSets ?? EmptySynSet;
            
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
                    case TaskType.merge:
                        var patchResult     = (MergeEntitiesResult)responseTask;
                        if (patchResult.errors == null)
                            continue;
                        container           = ((MergeEntities)task).container;
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
        /// These properties are set by <see cref="RemoteHostUtils.SetContainerResults"/>.
        private void GetContainerResults(SyncResponse response) {
            var containers = response.containers;
            if (containers == null) {
                return;
            }
            var processor = _intern.EntityProcessor();
            foreach (var container in containers) {
                if (!_intern.TryGetSetByName(container.container, out EntitySet set)) {
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
                    entityMap.Add(key, new EntityValue(key, entity));
                }
                entities.Clear();
                container.entities = null;
                
                // --- notFound
                if (notFound != null) {
                    foreach (var notFoundKey in notFound) {
                        entityMap.Add(notFoundKey, new EntityValue(notFoundKey));
                    }
                    notFound.Clear();
                    container.notFound = null;
                }
                
                // --- errors
                if (errors == null || errors.Count == 0)
                    continue;
                foreach (var error in errors) {
                    entityMap.Add(error.id, new EntityValue(error.id, error));
                }
                errors.Clear();
                container.errors = null;
            }
        }
        
        private SyncResult HandleSyncResponse(
            SyncRequest         syncRequest,
            ExecuteSyncResult   response,
            SyncStore           syncStore,
            MemoryBuffer        memoryBuffer)
        {
            using (var pooled = ObjectMapper.Get()) {
                SyncResult syncResult;
                try {
                    ProcessSyncTasks(syncRequest, response, syncStore, pooled.instance);
                    syncStore.DetectPatchesResults();
                }
                finally {
                    var functions   = syncStore.functions;
                    var failed      = GetFailedFunctions(functions);
                    syncResult      = _intern.syncResultBuffer.Get() ?? new SyncResult(this);
                    syncResult.Init(syncRequest, syncStore, memoryBuffer, functions, failed, response.error);
                    
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
        
        private static List<SyncFunction> GetFailedFunctions(List<SyncFunction> functions) {
            // create failed array only if required
            var errorCount  = 0;
            foreach (var task in functions) {
                if (!task.State.Error.HasErrors)
                    continue;
                errorCount++;
            }
            if (errorCount == 0) {
                return null;
            }
            var failed = new List<SyncFunction>(errorCount);
            foreach (var task in functions) {
                if (!task.State.Error.HasErrors)
                    continue;
                failed.Add(task);
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
                var emptyResults    = new List<ContainerEntities>();
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
            if (hub is SocketClientHub) {
                GetContainerResults(syncResponse);
            }
            var containers = syncResponse.containers;
            if (containers != null) {
                foreach (var containerEntities in containers) {
                    EntitySet set = _intern.GetSetByName(containerEntities.container);
                    set.SyncPeerEntityMap(containerEntities.entityMap, mapper);
                }
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
                ProcessTaskResult(task, result, syncStore, containers, mapper);
            }
        }
        
        private static void ProcessTaskResult (
            SyncRequestTask                         task,
            SyncTaskResult                          result,
            SyncStore                               syncStore,
            List<ContainerEntities>                 containerResults,
            ObjectMapper                            mapper)
        {
            var syncSets    = syncStore.SyncSets ?? EmptySynSet;
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
                    var entities = containerResults?.Find(c => c.container.IsEqual(readList.container));
                    syncSet.ReadEntitiesResult(readList, result, entities);
                    break;
                case TaskType.query:
                    var query =             (QueryEntities)     task;
                    syncSet = syncSets[query.container];
                    var queryEntities = containerResults?.Find(c => c.container.IsEqual(query.container));
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
                case TaskType.merge:
                    var patch =             (MergeEntities)     task;
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