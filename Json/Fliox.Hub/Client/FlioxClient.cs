// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.Hub.Client
{
    public readonly struct UserInfo {
                                    public  readonly    JsonKey     userId; 
        [DebuggerBrowsable(Never)]  public  readonly    string      token;
                                    public  readonly    JsonKey     clientId;

        public override     string      ToString() => $"userId: {userId}, clientId: {clientId}";

        public UserInfo (in JsonKey userId, string token, in JsonKey clientId) {
            this.userId     = userId;
            this.token      = token;
            this.clientId   = clientId;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [Fri.TypeMapper(typeof(FlioxClientMatcher))]
    public class FlioxClient : ITracerContext, IDisposable, IResetable
    {
        // Keep all FlioxClient fields in ClientIntern (_intern) to enhance debugging overview.
        // Reason:  FlioxClient is extended by application and add multiple EntitySet fields or properties.
        //          This ensures focus on fields / properties relevant for an application which are:
        //          StoreInfo, Tasks, ClientId & UserId
        // ReSharper disable once InconsistentNaming
        internal            ClientIntern                _intern;
        public              StoreInfo                   StoreInfo       => new StoreInfo(_intern.syncStore, _intern.setByType); 
        public   override   string                      ToString()      => StoreInfo.ToString();
        public              IReadOnlyList<SyncTask>     Tasks           => _intern.syncStore.appTasks;
        
        public              int                         GetSyncCount()  => _intern.syncCount;
        
        [DebuggerBrowsable(Never)]
        internal            ObjectPool<ObjectMapper>    ObjectMapper    => _intern.pool.ObjectMapper;

        // --- commands
        /// standard commands
        public readonly     StdCommands                 std;

        /// <summary>
        /// Instantiate a <see cref="FlioxClient"/> with a given <paramref name="hub"/>.
        /// </summary>
        public FlioxClient(FlioxHub hub, string database = null) {
            if (hub  == null)  throw new ArgumentNullException(nameof(hub));
            var eventTarget = new EventTarget(this);
            _intern = new ClientIntern(this, hub, database, this, eventTarget);
            std     = new StdCommands  (this);
        }
        
        public virtual void Dispose() {
            _intern.Dispose();
        }
        
        public static Type[] GetEntityTypes<TFlioxClient> () where TFlioxClient : FlioxClient {
            return ClientEntityUtils.GetEntityTypes<TFlioxClient>();
        }

        // --------------------------------------- public interface ---------------------------------------
        [DebuggerBrowsable(Never)]
        public bool WritePretty { set {
            foreach (var setPair in _intern.setByType) {
                setPair.Value.WritePretty = value;
            }
        } }
        
        [DebuggerBrowsable(Never)]
        public bool WriteNull { set {
            foreach (var setPair in _intern.setByType) {
                setPair.Value.WriteNull = value;
            }
        } }

        public void Reset() {
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.Reset();
            }
            _intern.Reset();
        }
        
        // --- SyncTasks() / TrySyncTasks()
        public async Task<SyncResult> SyncTasks() {
            var syncRequest     = CreateSyncRequest(out SyncStore syncStore);
            var executeContext  = new ExecuteContext(_intern.pool, _intern.eventTarget, _intern.sharedCache, _intern.clientId);
            var response        = await ExecuteSync(syncRequest, executeContext).ConfigureAwait(ClientUtils.OriginalContext);
            
            var result = HandleSyncResponse(syncRequest, response, syncStore);
            if (!result.Success)
                throw new SyncTasksException(response.error, result.failed);
            executeContext.Release();
            return result;
        }
        
        public async Task<SyncResult> TrySyncTasks() {
            var syncRequest     = CreateSyncRequest(out SyncStore syncStore);
            var executeContext  = new ExecuteContext(_intern.pool, _intern.eventTarget, _intern.sharedCache, _intern.clientId);
            var response        = await ExecuteSync(syncRequest, executeContext).ConfigureAwait(ClientUtils.OriginalContext);

            var result = HandleSyncResponse(syncRequest, response, syncStore);
            executeContext.Release();
            return result;
        }

        public string UserId {
            get => _intern.userId.AsString();
            set => _intern.userId = new JsonKey(value);
        }
        
        [DebuggerBrowsable(Never)]
        public string Token {
            get => _intern.token;
            set => _intern.token   = value;
        }

        public string ClientId {
            get => _intern.clientId.AsString();
            set => SetClientId(new JsonKey(value));
        }
        
        private void SetClientId(in JsonKey newClientId) {
            if (newClientId.IsEqual(_intern.clientId))
                return;
            if (!_intern.clientId.IsNull()) {
                _intern.hub.RemoveEventTarget(_intern.clientId);
            }
            _intern.clientId    = newClientId;
            if (!_intern.clientId.IsNull()) {
                _intern.hub.AddEventTarget(newClientId, _intern.eventTarget);
            }
        }

        [DebuggerBrowsable(Never)]
        public UserInfo UserInfo {
            get => new UserInfo (_intern.userId, _intern.token, _intern.clientId);
            set {
                _intern.userId  = value.userId;
                _intern.token   = value.token;
                SetClientId      (value.clientId);
            }
        }

        // --- LogChanges
        public LogTask LogChanges() {
            var task = _intern.syncStore.CreateLog();
            using (var pooled = ObjectMapper.Get()) {
                foreach (var setPair in _intern.setByType) {
                    EntitySet set = setPair.Value;
                    set.LogSetChangesInternal(task, pooled.instance);
                }
            }
            AddTask(task);
            return task;
        }
        
        
        // --- SubscribeAllChanges
        /// <summary>
        /// Subscribe to database changes of all <see cref="EntityContainer"/>'s with the given <paramref name="changes"/>.
        /// By default these changes are applied to the <see cref="FlioxClient"/>.
        /// To react on specific changes use <see cref="SetSubscriptionHandler"/>.
        /// To unsubscribe from receiving change events set <paramref name="changes"/> to null.
        /// </summary>
        public List<SyncTask> SubscribeAllChanges(IEnumerable<Change> changes) {
            AssertSubscriptionProcessor();
            var tasks = new List<SyncTask>();
            foreach (var setPair in _intern.setByType) {
                var set = setPair.Value;
                // ReSharper disable once PossibleMultipleEnumeration
                var task = set.SubscribeChangesInternal(changes);
                tasks.Add(task);
            }
            return tasks;
        }
        
        /// <summary>
        /// Set a custom <see cref="SubscriptionProcessor"/> to enable reacting on specific database change or message (or command) events.
        /// E.g. notifying other application modules about created, updated, deleted or patches entities.
        /// To subscribe to database change events use <see cref="EntitySet{TKey,T}.SubscribeChanges"/>.
        /// The default <see cref="SubscriptionProcessor"/> apply all changes to the <see cref="FlioxClient"/> as they arrive.
        /// To subscribe to message events use <see cref="SubscribeMessage"/>.
        /// <br></br>
        /// In contrast to <see cref="SetSubscriptionHandler"/> this method provide additional possibilities by the
        /// given <see cref="SubscriptionProcessor"/>. These are:
        /// <para>
        ///   Defer processing of events by queuing them for later processing.
        ///   E.g. by doing nothing in an override of <see cref="SubscriptionProcessor.ProcessEvent"/>.  
        /// </para>
        /// <para>
        ///   Manipulation of the received <see cref="EventMessage"/> in an override of
        ///   <see cref="SubscriptionProcessor.ProcessEvent"/> before processing it.
        /// </para>
        /// </summary>
        public void SetSubscriptionProcessor(SubscriptionProcessor subscriptionProcessor) {
            _intern.subscriptionProcessor = subscriptionProcessor ?? throw new NullReferenceException(nameof(subscriptionProcessor));
        }
        
        /// <summary>
        /// Set a <see cref="SubscriptionHandler"/> which is called for all events received by the client.
        /// These events fall in two categories:
        /// <para>
        ///   1. change events.
        ///      To receive change events use <see cref="SubscribeAllChanges"/> or
        ///      <see cref="EntitySet{TKey,T}.SubscribeChanges"/> and its sibling methods.
        /// </para>
        /// <para>
        ///   2. message/command events.
        ///      To receive message/command events use <see cref="SubscribeMessage"/> or sibling methods.
        /// </para>
        /// </summary>
        public void SetSubscriptionHandler(SubscriptionHandler handler) {
            AssertSubscriptionProcessor();
            _intern.subscriptionHandler = handler;
        }
        
        // --- SendMessage
        public MessageTask SendMessage(string name) {
            var task = new MessageTask(name, new JsonValue());
            _intern.syncStore.MessageTasks().Add(task);
            AddTask(task);
            return task;
        }
        
        public MessageTask SendMessage<TMessage>(string name, TMessage message) {
            using (var pooled = ObjectMapper.Get()) {
                var writer  = pooled.instance.writer;
                var json    = writer.WriteAsArray(message);
                var task    = new MessageTask(name, new JsonValue(json));
                _intern.syncStore.MessageTasks().Add(task);
                AddTask(task);
                return task;
            }
        }
        
        // --- SendCommand
        /// <summary>
        /// Send a command with the given <paramref name="name"/> (without a command value) to the attached <see cref="FlioxHub"/>.
        /// The method can be used directly for rapid prototyping. For production grade encapsulate call by a command method to
        /// the <see cref="FlioxClient"/> subclass. Doing this adds the command and its API to the <see cref="DatabaseSchema"/>. 
        /// </summary>
        public CommandTask<TResult> SendCommand<TResult>(string name) {
            var task    = new CommandTask<TResult>(name, new JsonValue(), _intern.pool);
            _intern.syncStore.MessageTasks().Add(task);
            AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Send a command with the given <paramref name="name"/> and <paramref name="param"/> value to the attached <see cref="FlioxHub"/>.
        /// The method can be used directly for rapid prototyping. For production grade encapsulate call by a command method to
        /// the <see cref="FlioxClient"/> subclass. Doing this adds the command and its API to the <see cref="DatabaseSchema"/>. 
        /// </summary>
        public CommandTask<TResult> SendCommand<TParam, TResult>(string name, TParam param) {
            using (var pooled = ObjectMapper.Get()) {
                var mapper  = pooled.instance;
                var json    = mapper.WriteAsArray(param);
                var task    = new CommandTask<TResult>(name, new JsonValue(json), _intern.pool);
                _intern.syncStore.MessageTasks().Add(task);
                AddTask(task);
                return task;
            }
        }

        // --- SubscribeMessage
        public SubscribeMessageTask SubscribeMessage<TMessage>  (string name, MessageSubscriptionHandler<TMessage> handler) {
            AssertSubscriptionProcessor();
            var callbackHandler = new GenericMessageCallback<TMessage>(name, handler);
            var task            = _intern.AddCallbackHandler(name, callbackHandler);
            AddTask(task);
            return task;
        }
        
        public SubscribeMessageTask SubscribeMessage            (string name, MessageSubscriptionHandler handler) {
            AssertSubscriptionProcessor();
            var callbackHandler = new NonGenericMessageCallback(name, handler);
            var task            = _intern.AddCallbackHandler(name, callbackHandler);
            AddTask(task);
            return task;
        }
        
        // --- UnsubscribeMessage
        public SubscribeMessageTask UnsubscribeMessage<TMessage>(string name, MessageSubscriptionHandler<TMessage> handler) {
            var task = _intern.RemoveCallbackHandler(name, handler);
            AddTask(task);
            return task;
        }
        
        public SubscribeMessageTask UnsubscribeMessage          (string name, MessageSubscriptionHandler handler) {
            var task = _intern.RemoveCallbackHandler(name, handler);
            AddTask(task);
            return task;
        }

        
        // ------------------------------------------- internals -------------------------------------------
        internal void AssertSubscriptionProcessor() {
            if (_intern.subscriptionProcessor != null)
                return;
            var msg = $"subscriptions require a {nameof(SubscriptionProcessor)} - {nameof(SetSubscriptionProcessor)}() before";
            throw new InvalidOperationException(msg);
        }
        
        public async Task CancelPendingSyncs() {
            foreach (var pair in _intern.pendingSyncs) {
                var executeContext = pair.Value;
                executeContext.Cancel();
            }
            await Task.WhenAll(_intern.pendingSyncs.Keys).ConfigureAwait(false);
        }
        
        public int GetPendingSyncsCount() {
            return _intern.pendingSyncs.Count;
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

        private static void SetErrors(SyncResponse response, SyncStore syncStore) {
            var syncSets = syncStore.SyncSets;
            var createErrors = response.createErrors;
            if (createErrors != null) {
                foreach (var createError in createErrors) {
                    createError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[createError.Key];
                    syncSet.errorsCreate = createError.Value.errors;
                }
            }
            var upsertErrors = response.upsertErrors;
            if (upsertErrors != null) {
                foreach (var upsertError in upsertErrors) {
                    upsertError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[upsertError.Key];
                    syncSet.errorsUpsert = upsertError.Value.errors;
                }
            }
            var patchErrors = response.patchErrors;
            if (patchErrors != null) {
                foreach (var patchError in patchErrors) {
                    patchError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[patchError.Key];
                    syncSet.errorsPatch = patchError.Value.errors;
                }
            }
            var deleteErrors = response.deleteErrors;
            if (deleteErrors != null) {
                foreach (var deleteError in deleteErrors) {
                    deleteError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[deleteError.Key];
                    syncSet.errorsDelete = deleteError.Value.errors;
                }
            }
        }
        
        /// Map <see cref="ContainerEntities.entities"/>, <see cref="ContainerEntities.notFound"/> and
        /// <see cref="ContainerEntities.errors"/> to <see cref="ContainerEntities.entityMap"/>.
        /// These properties are set by <see cref="RemoteHostHub.SetContainerResults"/>.
        private void GetContainerResults(SyncResponse response) {
            var containers     = response.containers;
            if (containers == null)
                return;
            response.containers = null;
            var resultMap   = response.resultMap = new Dictionary<string, ContainerEntities>(containers.Count);
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
                foreach (var errorPair in errors) {
                    var key = errorPair.Key;
                    entityMap.Add(key, new EntityValue(errorPair.Value));
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
                SetErrors(result, syncStore);
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

    /// Add const / static members here instead of <see cref="FlioxClient"/> to avoid showing members in debugger.
    internal static class ClientUtils {
        /// <summary>
        /// Process continuation of <see cref="FlioxClient.ExecuteSync"/> on caller context.
        /// This ensures modifications to entities are applied on the same context used by the caller. 
        /// </summary>
        internal const bool OriginalContext = true;       
    }
    
    public static class StoreExtension
    {
        public static FlioxClient Store(this ITracerContext store) {
            return (FlioxClient)store;
        }
    }
}