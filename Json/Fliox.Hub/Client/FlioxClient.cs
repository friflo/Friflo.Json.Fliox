// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Application classes extending <see cref="FlioxClient"/> offer two main functionalities:
    /// <list type="number">
    ///   <item>
    ///     Define a <b>database schema</b> by declaring its containers, commands and messages
    ///   </item>
    ///   <item>
    ///     Instances of a class extending <see cref="FlioxClient"/> are <b>database clients</b> providing
    ///     type-safe access to the database containers, commands and messages
    ///   </item>
    /// </list>
    /// Its containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>.<br/>
    /// Its messages are methods returning a <see cref="MessageTask"/>.<br/>
    /// <br/>
    /// Instances of a class extending <see cref="FlioxClient"/> can be used on server and client side.<br/>
    /// The <see cref="FlioxClient"/> features and utilization available at
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Client/README.md">Client README.md</a>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [TypeMapper(typeof(FlioxClientMatcher))]
    public partial class FlioxClient : IDisposable, IResetable, ILogSource
    {
    #region - members   
        // Keep all FlioxClient fields in ClientIntern (_intern) to enhance debugging overview.
        // Reason:  FlioxClient is extended by application and add multiple EntitySet fields or properties.
        //          This ensures focus on fields / properties relevant for an application which are:
        //          StoreInfo, Tasks, ClientId & UserId
        // ReSharper disable once InconsistentNaming
        internal            ClientIntern                _intern;
        public              string                      DatabaseName    => _intern.database ?? _intern.hub.DatabaseName;
        public              StoreInfo                   StoreInfo       => new StoreInfo(_intern.syncStore, _intern.setByType); 
        public              IReadOnlyList<SyncTask>     Tasks           => _intern.syncStore.appTasks;
        public   readonly   StdCommands                 std;
        
        public   override   string                      ToString()              => StoreInfo.ToString();
        public              int                         GetSyncCount()          => _intern.syncCount;
        public              int                         GetPendingSyncCount()   => _intern.pendingSyncs.Count;

        [DebuggerBrowsable(Never)]  public      bool                        WritePretty { set => SetWritePretty(value); }
        [DebuggerBrowsable(Never)]  public      bool                        WriteNull   { set => SetWriteNull(value); }
        [DebuggerBrowsable(Never)]  internal    readonly   Type             type;
        [DebuggerBrowsable(Never)]  internal    ObjectPool<ObjectMapper>    ObjectMapper    => _intern.pool.ObjectMapper;
        [DebuggerBrowsable(Never)]  public      IHubLogger                  Logger          => _intern.hubLogger;
        
        public static Type[] GetEntityTypes<TFlioxClient> () where TFlioxClient : FlioxClient => ClientEntityUtils.GetEntityTypes<TFlioxClient>();
        #endregion

        // --------------------------------------- public methods ---------------------------------------
    #region - initialize    
        /// <summary>
        /// Instantiate a <see cref="FlioxClient"/> with a given <paramref name="hub"/>.
        /// </summary>
        public FlioxClient(FlioxHub hub, string database = null) {
            if (hub  == null)  throw new ArgumentNullException(nameof(hub));
            type    = GetType();
            var eventTarget = new EventTarget(this);
            _intern = new ClientIntern(this, hub, database, eventTarget);
            std     = new StdCommands  (this);
            hub.sharedEnv.sharedCache.AddRootType(type);
        }
        
        public virtual void Dispose() {
            _intern.Dispose();
        }
        
        public void Reset() {
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.Reset();
            }
            _intern.Reset();
        }
        #endregion

    #region - sync tasks
        public async Task<SyncResult> SyncTasks() {
            var syncRequest = CreateSyncRequest(out SyncStore syncStore);
            var syncContext = new SyncContext(_intern.pool, _intern.eventTarget, _intern.sharedCache, _intern.clientId);
            var response    = await ExecuteSync(syncRequest, syncContext).ConfigureAwait(ClientUtils.OriginalContext);
            
            var result = HandleSyncResponse(syncRequest, response, syncStore);
            if (!result.Success)
                throw new SyncTasksException(response.error, result.failed);
            syncContext.Release();
            return result;
        }
        
        public async Task<SyncResult> TrySyncTasks() {
            var syncRequest = CreateSyncRequest(out SyncStore syncStore);
            var syncContext = new SyncContext(_intern.pool, _intern.eventTarget, _intern.sharedCache, _intern.clientId);
            var response    = await ExecuteSync(syncRequest, syncContext).ConfigureAwait(ClientUtils.OriginalContext);

            var result = HandleSyncResponse(syncRequest, response, syncStore);
            syncContext.Release();
            return result;
        }
        
        public async Task CancelPendingSyncs() {
            foreach (var pair in _intern.pendingSyncs) {
                var syncContext = pair.Value;
                syncContext.Cancel();
            }
            await Task.WhenAll(_intern.pendingSyncs.Keys).ConfigureAwait(false);
        }
        #endregion

    #region - user id, client id, token
        [DebuggerBrowsable(Never)]
        public string UserId {
            get => _intern.userId.AsString();
            set => _intern.userId = new JsonKey(value);
        }
        
        [DebuggerBrowsable(Never)]
        public string Token {
            get => _intern.token;
            set => _intern.token   = value;
        }

        [DebuggerBrowsable(Never)]
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

        public UserInfo UserInfo {
            get => new UserInfo (_intern.userId, _intern.token, _intern.clientId);
            set {
                _intern.userId  = value.userId;
                _intern.token   = value.token;
                SetClientId      (value.clientId);
            }
        }
        #endregion

    #region - detect all patches
        public DetectAllPatchesTask DetectAllPatches() {
            var task = _intern.syncStore.CreateDetectAllPatchesTask();
            using (var pooled = ObjectMapper.Get()) {
                foreach (var setPair in _intern.setByType) {
                    EntitySet set = setPair.Value;
                    set.DetectSetPatchesInternal(task, pooled.instance);
                }
            }
            AddTask(task);
            return task;
        }
        #endregion

    #region - subscribe all changes
        /// <summary>
        /// Subscribe to database changes of all <see cref="EntityContainer"/>'s with the given <paramref name="change"/>.
        /// By default these changes are applied to the <see cref="FlioxClient"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to null.
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        /// </summary>
        public List<SyncTask> SubscribeAllChanges(Change change, ChangeSubscriptionHandler handler) {
            AssertSubscription();
            var tasks = new List<SyncTask>();
            foreach (var setPair in _intern.setByType) {
                var set = setPair.Value;
                // ReSharper disable once PossibleMultipleEnumeration
                var task = set.SubscribeChangesInternal(change);
                tasks.Add(task);
            }
            _intern.changeSubscriptionHandler = handler; 
            return tasks;
        }
        #endregion

    #region - subscrition event handling
        /// <summary> <see cref="SubscriptionEventHandler"/> is called for all events received by a <see cref="FlioxClient"/></summary>
        [DebuggerBrowsable(Never)] public SubscriptionEventHandler SubscriptionEventHandler {
            get => _intern.subscriptionEventHandler;
            set => _intern.subscriptionEventHandler = value;
        }

        /// <summary>
        /// Set the <see cref="IEventProcessor"/> used to process subscription events subscribed by a <see cref="FlioxClient"/><br/>
        /// <br/>
        /// By default a <see cref="FlioxClient"/> uses a <see cref="DirectEventProcessor"/> to handle subscription events
        /// in the thread an event arrives.<br/>
        /// In case of an <b>UI</b> application consider using a <see cref="SynchronizationContextProcessor"/> used to process
        /// subscription events in the <b>UI</b> thread.
        /// </summary>
        public void SetEventProcessor(IEventProcessor eventProcessor) {
            _intern.eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
        }
        
        /// <summary>
        /// Set a custom <see cref="SubscriptionProcessor"/> to process subscribed database changes or messages (commands).<br/>
        /// E.g. notifying other application modules about created, updated, deleted or patches entities.
        /// To subscribe to database change events use <see cref="EntitySet{TKey,T}.SubscribeChanges"/>.
        /// To subscribe to message events use <see cref="SubscribeMessage"/>.
        /// </summary>
        internal void SetSubscriptionProcessor(SubscriptionProcessor subscriptionProcessor) {
            var processor = subscriptionProcessor ?? throw new ArgumentNullException(nameof(subscriptionProcessor));
            _intern.SetSubscriptionProcessor(processor);
        }
        #endregion

    #region - send message
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
        #endregion

    #region - send command
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
        /// Send a command with the given <paramref name="name"/> and <paramref name="param"/> value to the attached <see cref="FlioxHub"/>. <br/>
        /// The method can be used directly for rapid prototyping. For production grade encapsulate its call by a method added to
        /// the <see cref="FlioxClient"/> subclass. Doing this adds the command and its signature to the <see cref="DatabaseSchema"/>. 
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
        #endregion

    #region - subscribe message / command
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        public SubscribeMessageTask SubscribeMessage<TMessage>  (string name, MessageSubscriptionHandler<TMessage> handler) {
            AssertSubscription();
            var callbackHandler = new GenericMessageCallback<TMessage>(name, handler);
            var task            = _intern.AddCallbackHandler(name, callbackHandler);
            AddTask(task);
            return task;
        }
        
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        public SubscribeMessageTask SubscribeMessage            (string name, MessageSubscriptionHandler handler) {
            AssertSubscription();
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
        #endregion
    }
}