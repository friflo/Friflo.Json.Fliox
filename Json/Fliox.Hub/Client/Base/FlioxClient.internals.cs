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
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // --------------------------------- FlioxClient internals ---------------------------------
    [CLSCompliant(true)]
    public partial class FlioxClient
    {
    #region - internal methods
        internal Set    GetSetByName    (in ShortString name)               => _intern.SetByName[name];
        internal bool   TryGetSetByName (in ShortString name, out Set set)  => _intern.SetByName.TryGetValue(name, out set);
        
        internal Set CreateEntitySet(int index) {
            ref var entityInfo = ref _readonly.entityInfos[index];
            var instance = entityInfo.containerMember.CreateInstance(entityInfo.container, index, this);
            entitySets[index] = instance;
            _intern.SetByName[entityInfo.containerShort] = instance;
            return instance;
        }
        
        private string FormatToString() {
            var sb = new StringBuilder();
            sb.Append('\'');
            sb.Append(DatabaseName);
            sb.Append("'  ");
            ClientInfo.AppendTo(sb);
            return sb.ToString();
        }
        
        private void SetClientId(in ShortString newClientId) {
            if (newClientId.IsEqual(_intern.clientId))
                return;
            if (!_intern.clientId.IsNull()) {
                _readonly.hub.RemoveEventReceiver(_intern.clientId);
            }
            _intern.clientId    = newClientId;
            if (!_intern.clientId.IsNull()) {
                _readonly.hub.AddEventReceiver(newClientId, options.eventReceiver);
            }
        }
        
        internal void AssertSubscription() {
            if (options.eventReceiver == null) {
                var msg = $"The FlioxHub used by the client don't support PushEvents. hub: {_readonly.hub.GetType().Name}";
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
            foreach (var set in entitySets) {
                if (set == null) continue;
                if (set.GetSubscription() != null)
                    count++;
            }
            return count;
        }
        
        internal void AddTask(SyncTask task) {
            _intern.syncStore.tasks.Add(task);
        }
        
        internal Set GetEntitySet(in ShortString name) {
            if (TryGetSetByName(name, out var entitySet))
                return entitySet;
            throw new InvalidOperationException($"unknown EntitySet. name: {name}");
        }
        
        internal static void AssertTrackEntities(FlioxClient client, string methodName) {
            if (client.TrackEntities) {
                return;
            }
            var msg = $"{methodName}() requires {client.GetType().Name}.{nameof(TrackEntities)} = true";
            throw new InvalidOperationException(msg);
        }
        
        private SyncRequest CreateSyncRequest(out SyncStore syncStore) {
            var mapper = ObjectMapper();
            return CreateSyncRequest(out syncStore, mapper);
        }
        
        /// <summary>
        /// By default a new <see cref="MemoryBuffer"/> is created as its array may be used by the application
        /// at any time later. <br/>
        /// It returns a pooled <see cref="MemoryBuffer"/> only if the application calls <see cref="SyncResult.Reuse"/>
        /// indicating its safe to do so.
        /// </summary>
        private MemoryBuffer CreateMemoryBuffer() {
            return _intern.memoryBufferPool.Get() ?? new MemoryBuffer(Static.MemoryBufferCapacity);
        }
        
        private SyncContext CreateSyncContext(MemoryBuffer memoryBuffer) {
            var syncContext = _intern.syncContextBuffer.Get() ?? new SyncContext(_readonly.sharedEnv, options.eventReceiver); 
            syncContext.SetMemoryBuffer(memoryBuffer);
            syncContext.clientId            = _intern.clientId;
            syncContext.responseReaderPool  = _readonly.responseReaderPool?.Get().instance.Reuse();
            return syncContext;
        }
        
        private void ReuseSyncContext(SyncContext syncContext, SyncRequest syncRequest) {
            _readonly.responseReaderPool?.Return(syncContext.responseReaderPool);
            _intern.syncContextBuffer.Add(syncContext);
            syncContext.Init();
            syncRequest.tasks.Clear();
            _intern.syncRequestBuffer.Add(syncRequest);
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
            // create new SyncStore to collect future SyncTask's and execute them via the next SyncTasks() call 
            _intern.syncStore   = _intern.syncStoreBuffer.Get() ?? new SyncStore();
            
            var tasks           = syncStore.tasks;
            var syncRequest     = _intern.syncRequestBuffer.Get() ?? new SyncRequest();
            InitSyncRequest(syncRequest);
            var requestTasks    = syncRequest.tasks ?? new ListOne<SyncRequestTask>(tasks.Count);
            syncRequest.tasks   = requestTasks;
            var context         = new CreateTaskContext (mapper);
            foreach (var task in tasks.GetReadOnlySpan()) {
                var requestTask = task.CreateRequestTask(context);
                requestTasks.Add(requestTask);
            }
            return syncRequest;
        }
        
        private void InitSyncRequest(SyncRequest syncRequest) {
            syncRequest.database    = _readonly.databaseShort;
            syncRequest.userId      = _intern.userId;
            syncRequest.clientId    = _intern.clientId; 
            syncRequest.token       = _intern.token;
            syncRequest.eventAck    = _intern.lastEventSeq;
        }
        
        private static readonly IDictionary<JsonKey, EntityError> NoErrors = new EmptyDictionary<JsonKey, EntityError>();

        internal static IDictionary<JsonKey, EntityError> ErrorsAsMap(List<EntityError> errors, in ShortString container) {
            if (errors == null) {
                return NoErrors;
            }
            foreach (var error in errors) {
                // error .container is not serialized as it is redundant data.
                // Infer its value from containing error List
                error.container = container;
            }
            var errorMap = new Dictionary<JsonKey, EntityError>(errors.Count, JsonKey.Equality);
            foreach (var error in errors) {
                errorMap.Add(error.id, error);
            }
            return errorMap;
        }

        private SyncResult HandleSyncResponse(
            SyncRequest         syncRequest,
            ExecuteSyncResult   response,
            SyncStore           syncStore,
            MemoryBuffer        memoryBuffer)
        {
            SyncResult syncResult;
            try {
                ProcessSyncTasks(syncRequest, response, syncStore);
                syncStore.DetectPatchesResults();
            }
            finally {
                var tasks   = syncStore.tasks;
                var failed  = GetFailedFunctions(tasks);
                syncResult  = _intern.syncResultBuffer.Get() ?? new SyncResult(this);
                syncResult.Init(syncStore, memoryBuffer, tasks, failed, response.error);
                
                foreach (var task in tasks.GetReadOnlySpan()) {
                    var onSync  = task.OnSync;
                    if (onSync == null)
                        continue;
                    var taskError = task.State.Error.TaskError;
                    try {
                        onSync(taskError);
                    }
                    catch (Exception e) {
                        var error = $"OnSync Exception in {task.GetLabel()}";
                        Logger.Log(HubLog.Error, error, e);
                    }
                }
            }
            return syncResult;
        }
        
        private static ListOne<SyncTask> GetFailedFunctions(ListOne<SyncTask> tasks) {
            // create failed array only if required
            var errorCount  = 0;
            foreach (var task in tasks.GetReadOnlySpan()) {
                if (!task.State.Error.HasErrors)
                    continue;
                errorCount++;
            }
            if (errorCount == 0) {
                return null;
            }
            var failed = new ListOne<SyncTask>(errorCount);
            foreach (var task in tasks.GetReadOnlySpan()) {
                if (!task.State.Error.HasErrors)
                    continue;
                failed.Add(task);
            }
            return failed;
        }

        private static void ProcessSyncTasks(SyncRequest syncRequest, ExecuteSyncResult response, SyncStore syncStore)
        {
            var tasks       = syncRequest.tasks.GetReadOnlySpan();
            var taskCount   = tasks.Length;
            var syncTasks   = syncStore.tasks;
            var error       = response.error;
            
            if (error != null) {
                // ----------- handle ErrorResponse -----------
                var syncError       = new TaskErrorResult (TaskErrorType.SyncError, error.message);
                // process all task using by passing an error 
                for (int n = 0; n < taskCount; n++) {
                    var task        = tasks[n];
                    var syncTask    = syncTasks[n];
                    ProcessTaskResult(task, syncTask, syncError);
                }
                return;
            }
            // ----------- handle SyncResponse -----------
            response.success.AssertResponse(syncRequest);
            var responseTasks   = response.success.tasks;
            // Ensure every response task result type matches its task
            for (int n = 0; n < taskCount; n++) {
                var task        = tasks[n];
                var taskType    = task.TaskType;
                var result      = responseTasks[n];
                var actual      = result.TaskType;
                if (taskType == actual || actual == TaskType.error) {
                    var syncTask    = syncTasks[n];
                    ProcessTaskResult(task, syncTask, result);
                    continue;
                }
                var msg = $"Expect task type of response matches request. index:{n} expect: {taskType} actual: {actual}";
                throw new InvalidOperationException(msg);
            }
        }
        
        private static void ProcessTaskResult (
            SyncRequestTask         task,
            SyncTask                syncTasks,
            SyncTaskResult          result)
        {
            switch (task.TaskType) {
                case TaskType.reserveKeys: {
                    var reserveKeys =       (ReserveKeys)       task;
                    syncTasks.taskSet.ReserveKeysResult(reserveKeys, result);
                    break;
                }
                case TaskType.create: {
                    var create =            (CreateEntities)    task;
                    syncTasks.taskSet.CreateEntitiesResult(create, result);
                    break;
                }
                case TaskType.upsert: {
                    var upsert =            (UpsertEntities)    task;
                    syncTasks.taskSet.UpsertEntitiesResult(upsert, result);
                    upsert.entities.Clear();
                    syncTasks.taskSet.upsertEntitiesBuffer.Add(upsert);
                    break;
                }
                case TaskType.read: {
                    var read =              (ReadEntities)      task;
                    syncTasks.taskSet.ReadEntitiesResult(read, result);
                    read.ids.Clear();
                    syncTasks.taskSet.readEntitiesBuffer.Add(read);
                    break;
                }
                case TaskType.query: {
                    var query =             (QueryEntities)     task;
                    syncTasks.taskSet.QueryEntitiesResult(query, result);
                    break;
                }
                case TaskType.closeCursors: {
                    var closeCursors =      (CloseCursors)      task;
                    syncTasks.taskSet.CloseCursorsResult(closeCursors, result);
                    break;
                }
                case TaskType.aggregate: {
                    var aggregate =         (AggregateEntities) task;
                    syncTasks.taskSet.AggregateEntitiesResult(aggregate, result);
                    break;
                }
                case TaskType.merge: {
                    var patch =             (MergeEntities)     task;
                    syncTasks.taskSet.PatchEntitiesResult(patch, result);
                    break;
                }
                case TaskType.delete: {
                    var delete =            (DeleteEntities)    task;
                    syncTasks.taskSet.DeleteEntitiesResult(delete, result);
                    break;
                }
                case TaskType.message: {
                    var message =           (SendMessage)       task;
                    SyncStore.MessageResult(message, result);
                    break;
                }
                case TaskType.command: {
                    var command =           (SendCommand)       task;
                    SyncStore.MessageResult(command, result);
                    break;
                }
                case TaskType.subscribeChanges: {
                    var subscribeChanges =  (SubscribeChanges)  task;
                    syncTasks.taskSet.SubscribeChangesResult(subscribeChanges, result);
                    break;
                }
                case TaskType.subscribeMessage: {
                    var subscribeMessage =  (SubscribeMessage)  task;
                    SyncStore.SubscribeMessageResult(subscribeMessage, result);
                    break;
                }
            }
        }
        #endregion
    }
}