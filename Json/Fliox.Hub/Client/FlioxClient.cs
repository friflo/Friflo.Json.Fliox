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
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
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
    [Fri.TypeMapper(typeof(FlioxClientMatcher))]
    public partial class FlioxClient : ITracerContext, IDisposable, IResetable
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
        
        [DebuggerBrowsable(Never)]  internal    readonly   Type             type;
        [DebuggerBrowsable(Never)]  internal    ObjectPool<ObjectMapper>    ObjectMapper    => _intern.pool.ObjectMapper;
        [DebuggerBrowsable(Never)]  internal    HubLogger                   HubLogger       => _intern.hubLogger;

        // --- commands
        /// standard commands
        public readonly     StdCommands                 std;

        /// <summary>
        /// Instantiate a <see cref="FlioxClient"/> with a given <paramref name="hub"/>.
        /// </summary>
        public FlioxClient(FlioxHub hub, string database = null) {
            if (hub  == null)  throw new ArgumentNullException(nameof(hub));
            type    = GetType();
            var eventTarget = new EventTarget(this);
            _intern = new ClientIntern(this, hub, database, this, eventTarget);
            std     = new StdCommands  (this);
            hub.sharedEnv.sharedCache.AddRootType(type);
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