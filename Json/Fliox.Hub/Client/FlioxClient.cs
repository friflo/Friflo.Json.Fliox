// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// Note! Must not import
// using System.Threading.Tasks;    =>   async methods must be placed in FlioxClient.execute.async.cs

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Application classes extending <see cref="FlioxClient"/> offer two main functionalities: <br/>
    /// <b>1.</b> Define a <b>database schema</b> by declaring its containers, commands and messages <br/>
    /// <b>2.</b> Its instances are <b>database clients</b> providing type-safe access to database containers, commands and messages
    /// </summary>
    /// <remarks>
    /// Its containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>.<br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>.<br/>
    /// Its messages are methods returning a <see cref="MessageTask"/>.<br/>
    /// <see cref="FlioxClient"/> instances can be used in server and client code.<br/>
    /// The <see cref="FlioxClient"/> features and utilization available at
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Client/README.md">Client README.md</a><br/>
    /// <br/>
    /// <b>Info</b>:
    /// A single <see cref="FlioxClient"/> instance support multiple parallel async operations executed with <see cref="SyncTasks"/>.<br/>
    /// E.g. multiple <see cref="SyncTasks"/> can be called without immediate await and instead using <c>Task.WhenAll()</c> for all tasks. 
    /// </remarks>
    [TypeMapper(typeof(FlioxClientMatcher))]
    public partial class FlioxClient : IDisposable, IResetable, ILogSource
    {
    #region - public properties
    
        /// <summary> List of tasks created by its <see cref="FlioxClient"/> methods. These tasks are executed when calling <see cref="SyncTasks"/> </summary>
                        public      IReadOnlyList<SyncTask>     Tasks           => _intern.syncStore.tasks;

        /// <summary> name of the database the client is attached to </summary>
        [Browse(Never)] public      string                      DatabaseName    => _readonly.database;

        /// <summary> access to standard database commands - <see cref="StdCommands"/> </summary>
        // ReSharper disable once InconsistentNaming
        [Browse(Never)] public      StdCommands                 std             => _std ??= new StdCommands(this);

        /// <summary> general client information: attached database, the number of cached entities and scheduled <see cref="Tasks"/> </summary>
        [Browse(Never)] public      ClientInfo                  ClientInfo      => new ClientInfo(this); 

        [Browse(Never)] public      IHubLogger                  Logger          => _readonly.hubLogger;
        
        /// <summary> Return the number of calls to <see cref="SyncTasks"/> and <see cref="TrySyncTasks"/> </summary>
                        public      int                         GetSyncCount()  => _intern.syncCount;

        /// <summary> Return the number of pending <see cref="SyncTasks"/> and <see cref="TrySyncTasks"/> calls </summary>
                        public      int                         GetPendingSyncCount()   => _readonly.pendingSyncs.Count;
        
        [Browse(Never)] private     ClientOptions               options;
                        public      ref ClientOptions           Options         => ref options;
                        
                        public      bool                        TrackEntities   { get; init; } = true;
        
        /// <summary> Used to send typed messages / commands by classes extending <see cref="FlioxClient"/></summary>
        [Browse(Never)] protected readonly  SendTask            send;

        public override             string                      ToString()      => FormatToString();
        
        #endregion
        
    #region - user id, client id, token
    
        /// <summary>user id - identifies the user at a Hub</summary>
        [Browse(Never)] public      string   UserId      { get => _intern.userId.AsString();     set => _intern.userId = new ShortString(value); }
        
        /// <summary><see cref="Token"/> - used to authenticate the <see cref="UserId"/> at the Hub</summary>
        [Browse(Never)] public      string   Token       { get => _intern.token.AsString();      set => _intern.token = new ShortString(value); }

        /// <summary>client id - identifies the client at a Hub</summary>
        [Browse(Never)] public      string   ClientId    { get => _intern.clientId.AsString();   set => SetClientId(new ShortString(value)); }
        
        /// <summary>The tuple of <see cref="UserId"/>, <see cref="Token"/> and <see cref="ClientId"/></summary>
                        public UserInfo UserInfo    {
                            get => new UserInfo (_intern.userId, _intern.token, _intern.clientId);
                            set { _intern.userId = value.userId; _intern.token = value.token; SetClientId(value.clientId); }
                        }
        #endregion
        
    #region - internal fields
        // Keep most FlioxClient fields in _readonly / _intern to enhance debugging overview.
        // Reason:  FlioxClient is extended by application classes and add multiple EntitySet<,> fields or properties.
        //          This ensures focus on fields & properties relevant for an application which are:
        //          Tasks, UserInfo & EntitySet<,> fields
        // ReSharper disable once InconsistentNaming
                        internal readonly   ClientReadOnly  _readonly;

        // ReSharper disable once InconsistentNaming
                        internal            ClientIntern    _intern;        // Use intern struct as first field

        [Browse(Never)] internal readonly   Set[]           entitySets;
        
        // exposed only for access in debugger - not used by internally
        // ReSharper disable once UnusedMember.Local
                        private             FlioxHub        Hub             => _readonly.hub;
                        


        // ReSharper disable once InconsistentNaming
        [Browse(Never)] private             StdCommands     _std;

        [Browse(Never)] internal readonly   Type            type;

                        internal            ObjectMapper    ObjectMapper()  => objectMapper ??= new ObjectMapper(_readonly.typeStore);
        [Browse(Never)] private             ObjectMapper    objectMapper;

        
        /// <summary> using a static class prevents noise in form of 'Static members' for class instances in Debugger </summary>
        private static class Static {
            /// <summary>
            /// Process continuation of <see cref="FlioxClient.ExecuteAsync"/> on caller context. <br/>
            /// This ensures modifications to entities are applied on the same context used by the caller. <br/>
            /// It also ensures that <see cref="SyncTask.OnSync"/> is called on caller context. <br/>
            /// </summary>
            internal const  bool    OriginalContext = true;
            internal const  int     MemoryBufferCapacity = 1024;
        }

        #endregion

    // ----------------------------------------- public methods -----------------------------------------
    #region - initialize
        /// <summary>
        /// Instantiate a <see cref="FlioxClient"/> for the <paramref name="dbName"/> exposed by the given <paramref name="hub"/>.
        /// If <paramref name="dbName"/> is null the client uses the default database assigned to the <paramref name="hub"/>.
        /// </summary>
        public FlioxClient(FlioxHub hub, string dbName = null) {
            if (hub  == null)  throw new ArgumentNullException(nameof(hub));
            type                    = GetType();
            options.client          = this;
            options.eventReceiver   = hub.SupportPushEvents ? new ClientEventReceiver(this) : null;
            _readonly               = new ClientReadOnly(this, hub, dbName, out string typeError);
            if (typeError != null) {
                throw new InvalidTypeException(typeError);
            }
            _intern.Init(this);
            entitySets          = new Set[_readonly.entityInfos.Length];
            send                = new SendTask(this, _readonly.messagePrefix);
            hub.sharedEnv.sharedCache.AddRootType(type);
        }
        
        public virtual void Dispose() {
            _intern.Dispose();
        }
        
        /// <summary> Remove all tasks and all tracked entities of the <see cref="FlioxClient"/> </summary>
        public void Reset() {
            foreach (var set in entitySets) {
                set?.Reset();
            }
            _intern.Reset();
        }
        #endregion

    #region - detect all patches
        /// <summary>
        /// Detect the <b>Patches</b> made to all tracked entities in all <b>EntitySet</b>s of the client.
        /// Detected patches are applied to the database containers when calling <see cref="FlioxClient.SyncTasks"/>.        
        /// </summary>
        /// <remarks>
        /// Consider using one of the <see cref="EntitySet{TKey,T}.DetectPatches()"/> methods as this method
        /// run detection on all tracked entities in all <see cref="EntitySet{TKey,T}"/>s.
        /// </remarks>
        public DetectAllPatches DetectAllPatches() {
            AssertTrackEntities(this, nameof(DetectAllPatches));
            var task = _intern.syncStore.CreateDetectAllPatchesTask();
            var mapper = ObjectMapper();
            foreach (var set in entitySets) {
                set?.DetectSetPatchesInternal(task, mapper);
            }
            return task;
        }
        #endregion

    #region - subscription event handling
        /// <summary>
        /// <see cref="SubscriptionEventHandler"/> is called for all subscription events received by the <see cref="FlioxClient"/>
        /// </summary>
        [Browse(Never)] public SubscriptionEventHandler SubscriptionEventHandler {
            get => _intern.subscriptionEventHandler;
            set => _intern.subscriptionEventHandler = value;
        }

        /// <summary>
        /// Set the <see cref="EventProcessor"/> used to process subscription events subscribed by a <see cref="FlioxClient"/><br/>
        /// </summary>
        /// <remarks>
        /// By default a <see cref="FlioxClient"/> uses a <see cref="SynchronousEventProcessor"/> to handle subscription events
        /// in the thread an event arrives.<br/>
        /// In case of an <b>UI</b> application an <see cref="EventProcessorContext"/> can be used to process
        /// subscription events on the <b>UI</b> thread by using the <see cref="System.Threading.SynchronizationContext"/>
        /// of the application.
        /// </remarks>
        public void SetEventProcessor(EventProcessor eventProcessor) {
            _intern.eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
        }
        #endregion

    #region - subscribe all changes
        /// <summary>
        /// Subscribe to database changes of all <see cref="EntityContainer"/>'s with the given <paramref name="change"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to <see cref="Change.None"/>.
        /// </summary>
        /// <remarks>Note: To ensure remote clients with occasional disconnects get <b>all</b> events use <see cref="StdCommands.Client"/></remarks>
        /// <seealso cref="SetEventProcessor"/>
        public List<SyncTask> SubscribeAllChanges(Change change, ChangeSubscriptionHandler handler) {
            AssertSubscription();
            var tasks   = new List<SyncTask>();
            var sets    = entitySets;
            for (int n = 0; n < sets.Length; n++) {
                var entitySet = sets[n];
                if (entitySet == null) {
                    entitySet = CreateEntitySet(n);
                }
                var task = entitySet.SubscribeChangesInternal(change);
                tasks.Add(task);
            }
            _intern.changeSubscriptionHandler = handler; 
            return tasks;
        }
        #endregion

    #region - subscribe messages / commands
        /// <summary> Subscribe message / command with the given <paramref name="name"/> send to the database used by the client </summary>
        /// <remarks>Note: To ensure remote clients with occasional disconnects get <b>all</b> events use <see cref="StdCommands.Client"/></remarks>
        /// <seealso cref="SetEventProcessor"/>
        public SubscribeMessageTask SubscribeMessage<TMessage>  (string name, MessageSubscriptionHandler<TMessage> handler) {
            AssertSubscription();
            var callbackHandler = new GenericMessageCallback<TMessage>(name, handler);
            var task            = _intern.AddCallbackHandler(name, callbackHandler);
            AddTask(task);
            return task;
        }
        
        /// <summary> Subscribe message / command with the given <paramref name="name"/> send to the database used by the client </summary>
        /// <remarks>
        /// Subscribe multiple messages by prefix. e.g. <paramref name="name"/> = <c>"std.*"</c> <br/>
        /// Subscribe all messages with <paramref name="name"/> = <c>"*"</c> <br/>
        /// </remarks>
        /// <remarks>Note: To ensure remote clients with occasional disconnects get <b>all</b> events use <see cref="StdCommands.Client"/></remarks>
        /// <seealso cref="SetEventProcessor"/>
        public SubscribeMessageTask SubscribeMessage            (string name, MessageSubscriptionHandler handler) {
            AssertSubscription();
            var callbackHandler = new NonGenericMessageCallback(name, handler);
            var task            = _intern.AddCallbackHandler(name, callbackHandler);
            AddTask(task);
            return task;
        }
        
        // --- UnsubscribeMessage
        /// <summary> Remove subscription of message / command with the given <paramref name="name"/> send to the database used by the client </summary>
        /// <remarks> If <paramref name="handler"/> is null all subscription handlers are removed. </remarks>
        public SubscribeMessageTask UnsubscribeMessage<TMessage>(string name, MessageSubscriptionHandler<TMessage> handler) {
            var task = _intern.RemoveCallbackHandler(name, handler);
            AddTask(task);
            return task;
        }
        
        /// <summary> Remove subscription of message / command with the given <paramref name="name"/> send to the database used by the client </summary>
        /// <remarks>
        /// If <paramref name="handler"/> is null all subscription handlers are removed. <br/>
        /// Remove a prefix subscription with a trailing <c>*</c> E.g: <paramref name="name"/> = <c>"std.*"</c> or <c>"*"</c> <br/>
        /// </remarks>
        public SubscribeMessageTask UnsubscribeMessage          (string name, MessageSubscriptionHandler handler) {
            var task = _intern.RemoveCallbackHandler(name, handler);
            AddTask(task);
            return task;
        }
        #endregion

    #region - send message / command
        public readonly struct  SendTask {
            private readonly    FlioxClient client;
            private readonly    string      prefix;

            public  override    string      ToString()  => prefix;

            internal SendTask(FlioxClient client, string prefix) {
                this.client = client;
                this.prefix = string.IsNullOrEmpty(prefix) ? null : prefix;
            }

            /// <summary>
            /// Send a message with the name of the calling method and <paramref name="param"/> value to a database.<br/>
            /// Other clients can subscribe the message to receive an event with <see cref="SubscribeMessage"/>.
            /// </summary>
            public MessageTask          Message<TParam>          (TParam param, [CallerMemberName] string name = "") {
                var messageName = prefix == null ? name : prefix + name;
                return client.SendMessage(messageName, param);
            }
            
            /// <summary>
            /// Send a command with the name of the calling method (without a command value) to a database.<br/>
            /// Other clients can subscribe the command to receive an event with <see cref="SubscribeMessage"/>.
            /// </summary>
            public CommandTask<TResult> Command<TResult>         ([CallerMemberName] string name = "") {
                var commandName = prefix == null ? name : prefix + name;
                return client.SendCommand<TResult>(commandName);
            }
            
            /// <summary>
            /// Send a command with the name of the calling method and <paramref name="param"/> value to a database.<br/>
            /// Other clients can subscribe the command to receive an event with <see cref="SubscribeMessage"/>.
            /// </summary>
            public CommandTask<TResult> Command<TParam, TResult> (TParam param, [CallerMemberName] string name = "") {
                var commandName = prefix == null ? name : prefix + name;
                return client.SendCommand<TParam, TResult>(commandName, param);
            }
        }
        
        /// <summary>
        /// Send a message with the given <paramref name="name"/> (without a value) to a database.
        /// Other clients can subscribe the message to receive an event with <see cref="SubscribeMessage"/>.
        /// </summary>
        /// <remarks>
        /// The method can be used directly for rapid prototyping. <br/>For production grade code encapsulate call by adding a message method to
        /// the <see cref="FlioxClient"/> subclass. This adds the message and its API to the <see cref="DatabaseSchema"/>. 
        /// </remarks>
        public MessageTask SendMessage(string name) {
            var task = new MessageTask(new ShortString(name), new JsonValue());
            AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Send a message with the given <paramref name="name"/> and <paramref name="param"/> value to a database.
        /// Other clients can subscribe the message to receive an event with <see cref="SubscribeMessage"/>.
        /// </summary>
        /// <remarks>
        /// The method can be used directly for rapid prototyping. <br/> For production grade code encapsulate call by adding a message method to
        /// the <see cref="FlioxClient"/> subclass. Doing this adds the message and its signature to the <see cref="DatabaseSchema"/>. 
        /// </remarks>
        public MessageTask SendMessage<TMessage>(string name, TMessage param) {
            var writer  = ObjectMapper().writer;
            var json    = writer.WriteAsValue(param);
            var task    = new MessageTask(new ShortString(name), json);
            AddTask(task);
            return task;
        }

        /// <summary>
        /// Send a command with the given <paramref name="name"/> (without a command value) to a database.
        /// Other clients can subscribe the command to receive an event with <see cref="SubscribeMessage"/>.
        /// </summary>
        /// <remarks>
        /// The method can be used directly for rapid prototyping. <br/> For production grade code encapsulate call by adding a message method to
        /// the <see cref="FlioxClient"/> subclass. This adds the command and its API to the <see cref="DatabaseSchema"/>. 
        /// </remarks>
        public CommandTask<TResult> SendCommand<TResult>(string name) {
            var task    = new CommandTask<TResult>(new ShortString(name), new JsonValue(), _readonly.pool);
            AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Send a command with the given <paramref name="name"/> and <paramref name="param"/> value to a database.
        /// Other clients can subscribe the command to receive an event with <see cref="SubscribeMessage"/>.
        /// </summary>
        /// <remarks>
        /// The method can be used directly for rapid prototyping. <br/> For production grade code encapsulate call by adding a message method to
        /// the <see cref="FlioxClient"/> subclass. Doing this adds the command and its signature to the <see cref="DatabaseSchema"/>. 
        /// </remarks>
        public CommandTask<TResult> SendCommand<TParam, TResult>(string name, TParam param) {
            var writer  = ObjectMapper().writer;
            var json    = writer.WriteAsValue(param);
            var task    = new CommandTask<TResult>(new ShortString(name), json, _readonly.pool);
            AddTask(task);
            return task;
        }
        #endregion
        
    #region - non generic access
        /// <summary>
        /// Return the <see cref="Type"/>'s used by the <see cref="EntitySet{TKey,T}"/> members of a <see cref="FlioxClient"/> as entity Type. 
        /// </summary>
        public static Type[] GetEntityTypes(Type clientType)         => ClientEntityUtils.GetEntityTypes(clientType);

        /// <summary>
        /// Enable creation of strongly typed<see cref="EntitySet{TKey,T}"/>'s
        /// </summary>
        public static EntitySetInfo[] GetEntitySetInfos (FlioxClient client) {
            var infos   = client._readonly.entityInfos;
            var result  = new EntitySetInfo[infos.Length];
            for (int n = 0; n < infos.Length; n++) {
                result[n] = infos[n];
            }
            return result;
        }

        public static void GetRawEntities(FlioxClient client, string entitySet, List<object> result) {
            result.Clear();
            var name = new ShortString(entitySet);
            if (!client.TryGetSetByName(name, out Set set)) {
                return;
            }
            set.GetRawEntities(result);
        }
        #endregion
    }
}