// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Graph.Internal.Map;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Utils;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    internal struct StoreIntern
    {
        internal readonly   string                                  clientId;
        internal readonly   TypeStore                               typeStore;
        internal readonly   TypeStore                               ownedTypeStore;
        internal readonly   TypeCache                               typeCache;
        internal readonly   ObjectMapper                            jsonMapper;

        internal readonly   ObjectPatcher                           objectPatcher;
        
        internal readonly   EntityDatabase                          database;
        internal readonly   Dictionary<Type,   EntitySet>           setByType;
        internal readonly   Dictionary<string, EntitySet>           setByName;
        internal readonly   Pools                                   contextPools;
        internal readonly   EventTarget                             eventTarget;
        internal readonly   Dictionary<string, MessageSubscriber>   subscriptions;
        
        // --- non readonly
        internal            SyncStore                               sync;
        internal            LogTask                                 tracerLogTask;
        internal            SubscriptionHandler                     subscriptionHandler;
        internal            bool                                    disposed;
        internal            int                                     lastEventSeq;
        internal            int                                     syncCount;
        

        public   override   string                                  ToString() => clientId;


        internal StoreIntern(
            string                  clientId,
            TypeStore               typeStore,
            TypeStore               owned,
            EntityDatabase          database,
            ObjectMapper            jsonMapper,
            EventTarget             eventTarget,
            SyncStore               sync)
        {
            this.clientId               = clientId;
            this.typeStore              = typeStore;
            this.ownedTypeStore         = owned;
            this.database               = database;
            this.jsonMapper             = jsonMapper;
            this.typeCache              = jsonMapper.writer.TypeCache;
            this.eventTarget            = eventTarget;
            this.sync                   = sync;
            setByType                   = new Dictionary<Type, EntitySet>();
            setByName                   = new Dictionary<string, EntitySet>();
            objectPatcher               = new ObjectPatcher(jsonMapper);
            contextPools                = new Pools(Pools.SharedPools);
            subscriptions               = new Dictionary<string, MessageSubscriber>();
            tracerLogTask               = null;
            subscriptionHandler         = null;
            lastEventSeq                = 0;
            disposed                    = false;
            syncCount                   = 0;
        }
        
        internal Dictionary<string, MessageSubscriber> AddMessageHandler<TMessage> (string tag, Action<TMessage> action) {
            if (!subscriptions.TryGetValue(tag, out var subscriber)) {
                subscriber = new MessageSubscriber();
            }
            var messageHandler = new MessageHandler<TMessage>(action);
            subscriber.handlers.Add(messageHandler);
            return subscriptions;
        }
        
        internal Dictionary<string, MessageSubscriber> RemoveMessageHandler<TMessage> (string tag, Action<TMessage> action) {
            if (!subscriptions.TryGetValue(tag, out var subscriber)) {
                return subscriptions;
            }
            foreach (var handler in subscriber.handlers) {
                if (handler.HasAction(action))
                    subscriber.handlers.Remove(handler);
            }
            if (subscriber.handlers.Count == 0) {
                subscriptions.Remove(tag);
            }
            return subscriptions;
        }
    }

    // --------------------------------------- EntityStore ---------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class EntityStore : ITracerContext, IDisposable
    {
        // Keep all EntityStore fields in StoreIntern to enhance debugging overview.
        // Reason: EntityStore is extended by application and add multiple EntitySet fields.
        //         So internal fields are encapsulated in field intern.
        // ReSharper disable once InconsistentNaming
        internal            StoreIntern             _intern;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              TypeStore               TypeStore   => _intern.typeStore;
        public              StoreInfo               StoreInfo   => new StoreInfo(_intern.sync, _intern.setByType); 
        public   override   string                  ToString()  => StoreInfo.ToString();
        public              IReadOnlyList<SyncTask> Tasks       => _intern.sync.appTasks;
        
        public              EntitySet               GetEntitySet(string name)   => _intern.setByName[name];
        public              int                     GetSyncCount()              => _intern.syncCount;
        
        /// <summary>
        /// Instantiate an <see cref="EntityStore"/> with a given <see cref="database"/> and an optional <see cref="typeStore"/>.
        ///
        /// Optimization note:
        /// In case an application create many (> 10) <see cref="EntityStore"/> instances it should provide
        /// a <see cref="typeStore"/>. <see cref="TypeStore"/> instances are designed to be reused from multiple threads.
        /// Their creation is expensive compared to the instantiation of an <see cref="EntityStore"/>. 
        /// </summary>
        protected EntityStore(EntityDatabase database, TypeStore typeStore, string clientId) {
            TypeStore owned = null;
            if (typeStore == null) {
                typeStore = owned = new TypeStore();
            }
            AddTypeMappers(typeStore);
            
            // throw no exceptions on errors. Errors are handled by checking <see cref="ObjectReader.Success"/> 
            var jsonMapper = new ObjectMapper(typeStore, new NoThrowHandler()) {
                TracerContext = this
            };
            var eventTarget = new EventTarget(this);
            var sync = new SyncStore();
            _intern = new StoreIntern(clientId, typeStore, owned, database, jsonMapper, eventTarget, sync);
            database.AddEventTarget(clientId, eventTarget);
        }
        
        public void Dispose() {
            _intern.disposed = true;
            _intern.database.RemoveEventTarget(_intern.clientId);
            _intern.objectPatcher.Dispose();
            _intern.jsonMapper.Dispose();
            _intern.ownedTypeStore?.Dispose();
        }
        
        private static void AddTypeMappers (TypeStore typeStore) {
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
        }

        // --------------------------------------- public interface --------------------------------------- 
        public async Task Sync() {
            SyncRequest syncRequest = CreateSyncRequest();
            var messageContext = new MessageContext(_intern.contextPools, _intern.eventTarget, _intern.clientId);
            SyncResponse response = await ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            var result = HandleSyncResponse(syncRequest, response);

            if (!result.Success)
                throw new SyncResultException(response.error, result.failed);
            messageContext.pools.AssertNoLeaks();
        }
        
        public async Task<SyncResult> TrySync() {
            SyncRequest syncRequest = CreateSyncRequest();
            var messageContext = new MessageContext(_intern.contextPools, _intern.eventTarget, _intern.clientId);
            SyncResponse response = await ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            var result = HandleSyncResponse(syncRequest, response);
            messageContext.pools.AssertNoLeaks();
            return result;
        }
        
        /// <see cref="SyncWait"/> is redundant -> made private. Keep it for exploring (Unity)
        private void SyncWait() {
            SyncRequest syncRequest = CreateSyncRequest();
            var messageContext = new MessageContext(_intern.contextPools, _intern.eventTarget, _intern.clientId);
            var responseTask = ExecuteSync(syncRequest, messageContext);
            // responseTask.Wait();  
            SyncResponse response = responseTask.Result;  // <--- synchronous Sync point!!
            HandleSyncResponse(syncRequest, response);
            messageContext.pools.AssertNoLeaks();
        }

        public LogTask LogChanges() {
            var task = _intern.sync.CreateLog();
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.LogSetChangesInternal(task);
            }
            AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Subscribe to database changes of all <see cref="EntityContainer"/>'s with the given <see cref="changes"/>.
        /// By default these changes are applied to the <see cref="EntityStore"/>.
        /// To react on specific changes use <see cref="SetSubscriptionHandler"/>.
        /// To unsubscribe from receiving change events set <see cref="changes"/> to null.
        /// </summary>
        public List<SyncTask> SubscribeAllChanges(IEnumerable<Change> changes) {
            AssertSubscriptionHandler();
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
        /// Filter all <see cref="SendMessageText"/> messages by the given <see cref="tags"/>.
        ///   <para><see cref="tags"/> = {""} => subscribe all message events.</para>
        ///   <para><see cref="tags"/> = {} => unsubscribe message events.</para>
        /// </summary>
        public SubscribeMessagesTask SubscribeMessages(IEnumerable<string> tags) {
            AssertSubscriptionHandler();
            var subscriptions = _intern.subscriptions;
            subscriptions.Clear();
            foreach (var tag in tags) {
                subscriptions.Add(tag, null);
            }
            var task = new SubscribeMessagesTask(subscriptions.Keys);
            _intern.sync.subscribeMessages = task;
            AddTask(task);
            return task;
        }
        
        public SubscribeMessagesTask SubscribeMessage<TMessage>(Action<TMessage> action) {
            AssertSubscriptionHandler();
            const string tag    = nameof(TMessage);
            var subscriptions   = _intern.AddMessageHandler(tag, action);
            var task = new SubscribeMessagesTask(subscriptions.Keys);
            _intern.sync.subscribeMessages = task;
            AddTask(task);
            return task;
        }
        
        public SubscribeMessagesTask UnsubscribeMessage<TMessage>(Action<TMessage> action) {
            AssertSubscriptionHandler();
            const string tag    = nameof(TMessage);
            var subscriptions   = _intern.RemoveMessageHandler(tag, action);
            var task = new SubscribeMessagesTask(subscriptions.Keys);
            _intern.sync.subscribeMessages = task;
            AddTask(task);
            return task;
        }
        
        public MessageTask SendMessageText(string text) {
            var task = new MessageTask(text, null);
            _intern.sync.messageTasks.Add(text, task);
            AddTask(task);
            return task;
        }
        
        public MessageTask SendMessage<TMessage>(TMessage message) {
            var value           = _intern.jsonMapper.Write(message);
            const string tag    = nameof(Message);
            var task            = new MessageTask(tag, value);
            _intern.sync.messageTasks.Add(tag, task);
            AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Set a custom <see cref="SubscriptionHandler"/> to enable reacting on specific database change events.
        /// E.g. notifying other application modules about created, updated, deleted or patches entities.
        /// To subscribe to database change events <see cref="Graph.EntitySet{T}.SubscribeChanges"/> need to be called before.
        /// The default <see cref="SubscriptionHandler"/> apply all changes to the <see cref="EntityStore"/> as they arrive. 
        /// </summary>
        public void SetSubscriptionHandler(SubscriptionHandler subscriptionHandler) {
            _intern.subscriptionHandler = subscriptionHandler;
        }
        
        // ------------------------------------------- internals -------------------------------------------
        internal void AssertSubscriptionHandler() {
            if (_intern.subscriptionHandler == null)
                throw new InvalidOperationException("subscriptions require a SubscriptionHandler. SetSubscriptionHandler() before");
        }
        
        private async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            _intern.syncCount++;
            SyncResponse response;
            try {
                response = await _intern.database.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                response = new SyncResponse{error = new ErrorResponse{message = errorMsg}};
            }
            return response;
        }
        
        private int GetSubscriptionCount() {
            int count = 0;
            foreach (var setPair in _intern.setByType) {
                var set = setPair.Value;
                if (set.GetSubscription() != null)
                    count++;
            }
            return count;
        }
        
        internal void AddTask(SyncTask task) {
            _intern.sync.appTasks.Add(task);
        }
        
        internal EntitySet<T> EntitySet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (_intern.setByType.TryGetValue(entityType, out EntitySet set))
                return (EntitySet<T>)set;
            
            set = new EntitySet<T>(this);
            return (EntitySet<T>)set;
        }

        private SyncRequest CreateSyncRequest() {
            var tasks       = new List<DatabaseTask>();
            var syncRequest = new SyncRequest {
                tasks       = tasks,
                clientId    = _intern.clientId
            };
            var subscriptionCount = GetSubscriptionCount();
            if (subscriptionCount > 0) {
                syncRequest.eventAck = _intern.lastEventSeq;
            }

            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                var setInfo = set.SetInfo;
                var curTaskCount = tasks.Count;
                set.Sync.AddTasks(tasks);
                AssertTaskCount(setInfo, tasks.Count - curTaskCount);
            }
            _intern.sync.AddTasks(tasks);
            return syncRequest;
        }

        [Conditional("DEBUG")]
        private static void AssertTaskCount(SetInfo setInfo, int taskCount) {
            int expect  = setInfo.tasks; 
            if (expect != taskCount)
                throw new InvalidOperationException($"Unexpected task.Count. expect: {expect}, got: {taskCount}");
        }

        private void SetErrors(SyncResponse response) {
            var createErrors = response.createErrors;
            if (createErrors != null) {
                foreach (var createError in createErrors) {
                    createError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[createError.Key];
                    set.Sync.createErrors = createError.Value.errors;
                }
            }
            var updateErrors = response.updateErrors;
            if (updateErrors != null) {
                foreach (var updateError in updateErrors) {
                    updateError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[updateError.Key];
                    set.Sync.updateErrors = updateError.Value.errors;
                }
            }
            var patchErrors = response.patchErrors;
            if (patchErrors != null) {
                foreach (var patchError in patchErrors) {
                    patchError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[patchError.Key];
                    set.Sync.patchErrors = patchError.Value.errors;
                }
            }
            var deleteErrors = response.deleteErrors;
            if (deleteErrors != null) {
                foreach (var deleteError in deleteErrors) {
                    deleteError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[deleteError.Key];
                    set.Sync.deleteErrors = deleteError.Value.errors;
                }
            }
        }

        private SyncResult HandleSyncResponse(SyncRequest syncRequest, SyncResponse response) {
            SyncResult      syncResult;
            ErrorResponse   error = response.error;
            try {
                TaskErrorResult                         syncError;
                Dictionary<string, ContainerEntities>   containerResults;
                if (error == null) {
                    response.AssertResponse(syncRequest);
                    syncError = null;
                    containerResults = response.results;
                    foreach (var containerResult in containerResults) {
                        ContainerEntities containerEntities = containerResult.Value;
                        var set = _intern.setByName[containerResult.Key];
                        set.SyncPeerEntities(containerEntities.entities);
                    }
                    SetErrors(response);
                } else {
                    syncError = new TaskErrorResult {
                        message = error.message,
                        type    = TaskErrorResultType.SyncError
                    };
                    containerResults = new Dictionary<string, ContainerEntities>();
                }

                var tasks = syncRequest.tasks;
                var results = response.tasks;
                for (int n = 0; n < tasks.Count; n++) {
                    var task = tasks[n];
                    TaskType    taskType = task.TaskType;
                    TaskResult  result;
                    if (syncError == null) {
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

                    switch (taskType) {
                        case TaskType.create:
                            var create = (CreateEntities) task;
                            EntitySet set = _intern.setByName[create.container];
                            set.Sync.CreateEntitiesResult(create, result);
                            break;
                        case TaskType.update:
                            var update = (UpdateEntities) task;
                            set = _intern.setByName[update.container];
                            set.Sync.UpdateEntitiesResult(update, result);
                            break;
                        case TaskType.read:
                            var readList = (ReadEntitiesList) task;
                            set = _intern.setByName[readList.container];
                            containerResults.TryGetValue(readList.container, out ContainerEntities entities);
                            set.Sync.ReadEntitiesListResult(readList, result, entities);
                            break;
                        case TaskType.query:
                            var query = (QueryEntities) task;
                            set = _intern.setByName[query.container];
                            containerResults.TryGetValue(query.container, out ContainerEntities queryEntities);
                            set.Sync.QueryEntitiesResult(query, result, queryEntities);
                            break;
                        case TaskType.patch:
                            var patch = (PatchEntities) task;
                            set = _intern.setByName[patch.container];
                            set.Sync.PatchEntitiesResult(patch, result);
                            break;
                        case TaskType.delete:
                            var delete = (DeleteEntities) task;
                            set = _intern.setByName[delete.container];
                            set.Sync.DeleteEntitiesResult(delete, result);
                            break;
                        case TaskType.message:
                            var message = (Message) task;
                            _intern.sync.MessageResult(message, result);
                            break;
                        case TaskType.subscribeChanges:
                            var subscribe = (SubscribeChanges) task;
                            set = _intern.setByName[subscribe.container];
                            set.Sync.SubscribeChangesResult(subscribe, result);
                            break;
                        case TaskType.subscribeMessages:
                            var subscribeMessages = (SubscribeMessages) task;
                            _intern.sync.SubscribeMessagesResult(subscribeMessages, result);
                            break;
                    }
                }
                _intern.sync.LogResults();
            }
            finally {
                var failed = new List<SyncTask>();
                foreach (SyncTask task in _intern.sync.appTasks) {
                    task.AddFailedTask(failed);
                }
                syncResult = new SyncResult(_intern.sync.appTasks, failed, error);
                // new EntitySet task are collected (scheduled) in a new EntitySetSync instance and requested via next Sync() 
                foreach (var setPair in _intern.setByType) {
                    EntitySet set = setPair.Value;
                    set.ResetSync();
                }
                _intern.sync = new SyncStore();
            }
            return syncResult;
        }
    }
    
    public static class StoreExtension
    {
        public static EntityStore Store(this ITracerContext store) {
            return (EntityStore)store;
        }
    }
}
