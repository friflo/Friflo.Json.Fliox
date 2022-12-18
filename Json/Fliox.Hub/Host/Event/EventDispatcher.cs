// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// Specify the way in which events are send to their targets by an <see cref="EventDispatcher"/><br/>
    /// Events are generated from the database changes and messages send to a <see cref="FlioxHub"/>
    /// </summary>
    public enum EventDispatching
    {
        /// <summary>Events are queued and send asynchronously to their targets</summary>
        QueueSend,
        /// <summary>
        /// Events are queued only.<br/>
        /// The application need to call <see cref="EventDispatcher.SendQueuedEvents"/> regularly
        /// to send events asynchronously to their targets.
        /// </summary>
        Queue,
        /// <summary>
        /// Events are instantaneously send to their targets when processing a request in <see cref="FlioxHub.ExecuteRequestAsync"/>.
        /// </summary>
        Send
    }
    
    /// <summary>
    /// An <see cref="EventDispatcher"/> is used to enable Pub-Sub.
    /// </summary>
    /// <remarks>
    /// If assigned to <see cref="FlioxHub.EventDispatcher"/> the <see cref="FlioxHub"/> send
    /// push events to clients for database changes and messages these clients have subscribed. <br/>
    /// In case of remote database connections <b>WebSockets</b> are used to send push events to clients.
    /// </remarks>
    public sealed class EventDispatcher : IDisposable
    {
        private  readonly   SharedEnv                                       sharedEnv;
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        private  readonly   List<RemoteSyncEvent>                           eventBuffer;
        private  readonly   EventMessage                                    eventMessage;
        //
        /// key: <see cref="EventSubClient.clientId"/>
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<JsonKey, EventSubClient>   subClients;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubClient>                     SubClients  => subClients.Values;
        
        /// <summary> Subset of <see cref="subClients"/> eligible for sending events. Either they
        /// are <see cref="EventSubClient.Connected"/> or they <see cref="EventSubClient.queueEvents"/> </summary> 
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<JsonKey, EventSubClient>   sendClients;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubClient>                     SendClients  => subClients.Values;
        //
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<JsonKey, EventSubUser>     subUsers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubUser>                       SubUsers    => subUsers.Values;
        //
        /// <summary> exposed only for test assertions. <see cref="EventDispatcher"/> lives on Hub. <br/>
        /// If required its state (subscribed client) can be exposed by <see cref="DB.Monitor.ClientHits"/></summary>
        [DebuggerBrowsable(Never)]
        public              int                                             SubscribedClientsCount => subClients.Count;
        //
        internal readonly   EventDispatching                                dispatching;
        /// <see cref="clientEventLoop"/> and <see cref="clientEventWriter"/>
        /// are used as a queue to send pending <see cref="SyncEvent"/>'s
        private  readonly   Task                                            clientEventLoop;
        private  readonly   IDataChannelWriter<EventSubClient>              clientEventWriter;

        public   override   string                                          ToString() => $"subscribers: {subClients.Count}";

        private const string MissingEventReceiver = "subscribing events requires an eventReceiver. E.g a WebSocket as a target for push events.";

        public EventDispatcher (EventDispatching dispatching, SharedEnv env = null) {
            sharedEnv           = env ?? SharedEnv.Default;
            jsonEvaluator       = new JsonEvaluator();
            subClients          = new ConcurrentDictionary<JsonKey, EventSubClient>(JsonKey.Equality);
            sendClients         = new ConcurrentDictionary<JsonKey, EventSubClient>(JsonKey.Equality);
            subUsers            = new ConcurrentDictionary<JsonKey, EventSubUser>(JsonKey.Equality);
            eventBuffer         = new List<RemoteSyncEvent>();
            eventMessage        = new EventMessage { events = new List<SyncEvent>() };
            this.dispatching    = dispatching;
            if (dispatching == EventDispatching.QueueSend) {
                var channel             = DataChannelSlim<EventSubClient>.CreateUnbounded(true, true);
                clientEventWriter       = channel.Writer;
                var clientEventReader   = channel.Reader;
                clientEventLoop         = RunSendEventLoop(clientEventReader);
            }
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
        }
        
        internal bool TryGetSubscriber(in JsonKey key, out EventSubClient subClient) {
            return subClients.TryGetValue(key, out subClient);
        }
        
        /// used for test assertion
        public int QueuedEventsCount() {
            int count = 0;
            foreach (var pair in subClients) {
                count += pair.Value.QueuedEventsCount;
            }
            return count;
        }

        public async Task StopDispatcher() {
            if (dispatching != EventDispatching.QueueSend)
                return;
            StopQueue();
            await clientEventLoop.ConfigureAwait(false);
        }
        
        private void StopQueue() {
            NewClientEvent(null);
            clientEventWriter.Complete();
        }
        
        // -------------------------------- add / remove subscriptions --------------------------------
        internal bool SubscribeMessage(
            in SmallString      database,
            SubscribeMessage    subscribe,
            User                user,
            in JsonKey          clientId,
            EventReceiver       eventReceiver,
            out string          error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubClient subClient;
            var remove = subscribe.remove;
            if (remove.HasValue && remove.Value) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return true;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    return true;
                }
                databaseSubs.RemoveMessageSubscription(subscribe.name);
                RemoveEmptySubClient(subClient);
                return true;
            } else {
                subClient = GetOrCreateSubClient(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database.value);
                    subClient.databaseSubs.TryAdd(database, databaseSubs);
                }
                databaseSubs.AddMessageSubscription(subscribe.name);
                return true;
            }
        }

        internal bool SubscribeChanges (
            in SmallString      database,
            SubscribeChanges    subscribe,
            User                user,
            in JsonKey          clientId,
            EventReceiver       eventReceiver,
            out string          error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubClient subClient;
            if (subscribe.changes.Count == 0) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return true;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                    return true;
                databaseSubs.RemoveChangeSubscription(subscribe.container);
                RemoveEmptySubClient(subClient);
                return true;
            } else {
                subClient = GetOrCreateSubClient(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database.value);
                    subClient.databaseSubs.TryAdd(database, databaseSubs);
                }
                databaseSubs.AddChangeSubscription(subscribe);
                return true;
            }
        }
        
        internal EventSubClient GetOrCreateSubClient(User user, in JsonKey clientId, EventReceiver eventReceiver) {
            subClients.TryGetValue(clientId, out EventSubClient subClient);
            if (subClient != null) {
                // add to sendClients as the client could have been removed meanwhile caused by a disconnect
                sendClients.TryAdd(clientId, subClient);
                return subClient;
            }
            if (!subUsers.TryGetValue(user.userId, out var subUser)) {
                subUser = new EventSubUser (user.userId, user.GetGroups());
                subUsers.TryAdd(user.userId, subUser);
            }
            var dispatcher = dispatching == EventDispatching.QueueSend ? this : null;
            subClient = new EventSubClient(sharedEnv, subUser, clientId, dispatcher);
            if (eventReceiver != null) {
                subClient.UpdateTarget(eventReceiver);
            }
            subClients. TryAdd(clientId, subClient);
            sendClients.TryAdd(clientId, subClient);
            subUser.clients.TryAdd(subClient, true);
            return subClient;
        }

        /// <summary>
        /// Don't remove empty subClient as the state of <see cref="EventSubClient.eventCounter"/> need to be preserved.
        /// </summary>
        private void RemoveEmptySubClient(EventSubClient subClient) {
            /* if (subClient.SubCount > 0)
                return;
            subClients.TryRemove(subClient.clientId, out _);
            var user = subClient.user;
            user.clients.Remove(subClient);
            if (user.clients.Count == 0) {
                subUsers.TryRemove(user.userId, out _);
            } */
        }
        
        internal void UpdateSubUserGroups(in JsonKey userId, IReadOnlyCollection<String> groups) {
            if (!subUsers.TryGetValue(userId, out var subUser))
                return;
            subUser.groups.Clear();
            if (groups != null) {
                subUser.groups.UnionWith(groups);
            }
        }

        // -------------------------- event distribution --------------------------------
        public void SendQueuedEvents() {
            if (dispatching == EventDispatching.QueueSend) {
                throw new InvalidOperationException($"must not be called if using {nameof(EventDispatcher)}.{EventDispatching.QueueSend}");
            }
            using (var pooleMapper = sharedEnv.Pool.ObjectMapper.Get()) {
                var args = new SendEventArgs (pooleMapper.instance, eventMessage, eventBuffer);
                foreach (var pair in subClients) {
                    var subClient = pair.Value;
                    subClient.SendEvents(args);
                }
            }
        }
        
        private void ProcessSubscriber(SyncRequest syncRequest, SyncContext syncContext) {
            ref JsonKey  clientId = ref syncContext.clientId;
            if (clientId.IsNull())
                return;
            
            if (!subClients.TryGetValue(clientId, out var subClient))
                return;
            var eventReceiver = syncContext.eventReceiver;
            if (eventReceiver != null && eventReceiver.IsRemoteTarget()) {
                if (subClient.UpdateTarget (eventReceiver)) {
                    // remote client is using a new connection (WebSocket) so add to sendClients again
                    sendClients.TryAdd(subClient.clientId, subClient);
                }
            }
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            if (!syncContext.authState.hubPermission.queueEvents)
                 return;
            int value =  eventAck.Value;
            subClient.AcknowledgeEvents(value);
        }
        
        private static bool HasSubscribableTask(List<SyncRequestTask> tasks) {
            foreach (var task in tasks) {
                switch (task.TaskType) {
                    case TaskType.message:
                    case TaskType.command:
                    case TaskType.create:
                    case TaskType.upsert:
                    case TaskType.delete:
                    case TaskType.merge:
                        return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Create <see cref="SyncEvent"/>'s for the passed <see cref="SyncRequest.tasks"/> for
        /// all <see cref="EventSubClient"/>'s having matching <see cref="DatabaseSubs"/>
        /// </summary>
        internal void EnqueueSyncTasks (SyncRequest syncRequest, SyncContext syncContext) {
            var syncTasks = syncRequest.tasks;
            ProcessSubscriber (syncRequest, syncContext);

            if (sendClients.IsEmpty || !HasSubscribableTask(syncTasks)) {
                return; // early out
            }
            using (var pooled = syncContext.ObjectMapper.Get()) {
                ObjectWriter writer     = pooled.instance.writer;
                var database            = syncContext.databaseName;
                writer.Pretty           = false;    // write sub's as one liner
                writer.WriteNullMembers = false;
                
                foreach (var pair in sendClients) {
                    EventSubClient subClient = pair.Value;
                    if (!subClient.queueEvents && !subClient.Connected) {
                        sendClients.TryRemove(subClient.clientId, out _);
                        continue;
                    }
                    if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                        continue;
                    
                    var serializeEvents = SerializeRemoteEvents && subClient.SerializeEvents;
                    var buffer          = serializeEvents ? syncContext.syncBuffers.eventTasks : null;                    
                    var eventTasks      = databaseSubs.AddEventTasks(syncTasks, subClient, buffer, jsonEvaluator);
                    if (eventTasks == null)
                        continue;
                    // mark change events for (change) tasks which are sent by the client itself
                    bool?   isOrigin    = syncContext.clientId.IsEqual(subClient.clientId) ? true : (bool?)null;
                    var syncEvent = new SyncEvent { db = database.value, tasks = eventTasks, srcUserId = syncRequest.userId, isOrigin = isOrigin };
                    
                    if (serializeEvents) {
                        SerializeRemoteEvent(ref syncEvent, eventTasks, writer);
                    }
                    subClient.EnqueueEvent(ref syncEvent, serializeEvents, writer);
                }
            }
        }
        
        // ------------------------------- send client events -------------------------------
        internal void NewClientEvent(EventSubClient client) {
            bool success = clientEventWriter.TryWrite(client);
            if (success)
                return;
            Debug.Fail("NewClientEvent() - clientEventWriter.TryWrite() failed");
        }
        
        /// <summary>
        /// Loop is purely I/O bound => don't wrap in
        /// return Task.Run(async () => { ... });
        /// </summary>
        /// <seealso cref="WebSocketHost.RunReceiveMessageLoop"/>
        private async Task RunSendEventLoop(IDataChannelReader<EventSubClient> clientEventReader) {
            try {
                using (var mapper = new ObjectMapper(sharedEnv.TypeStore)) {
                    await SendEventLoop(clientEventReader, mapper).ConfigureAwait(false);
                }
            } catch (Exception e) {
                var message = "RunSendEventLoop() failed";
                sharedEnv.Logger.Log(HubLog.Error, message, e);
                Debug.Fail(message, e.Message);
            }
        }
        
        private async Task SendEventLoop(IDataChannelReader<EventSubClient> clientEventReader, ObjectMapper mapper) {
            var logger  = sharedEnv.Logger;
            var args    = new SendEventArgs(mapper, eventMessage, eventBuffer);
            while (true) {
                var client = await clientEventReader.ReadAsync().ConfigureAwait(false);
                if (client != null) {
                    client.SendEvents(args);
                    continue;
                }
                logger.Log(HubLog.Info, $"ClientEventLoop() returns");
                return;
            }
        }

        // --------------------------- serialize remote event optimization ---------------------------
        internal static bool SerializeRemoteEvents = true; // set to false for development

        /// Optimization: For remote connections the tasks are serialized to <see cref="SyncEvent.tasksJson"/>.
        /// Benefits of doing this:
        /// - serialize a task only once for multiple targets
        /// - storing only a single byte[] for a task instead of a complex SyncRequestTask which is not used anymore
        private static void SerializeRemoteEvent(ref SyncEvent syncEvent, List<SyncRequestTask> tasks, ObjectWriter writer) {
            var tasksJson = new JsonValue [tasks.Count];
            syncEvent.tasksJson = tasksJson;
            for (int n = 0; n < tasks.Count; n++) {
                var task = tasks[n];
                if (task.intern.json == null) {
                    // create an individual byte array.
                    // This is necessary as multiple arrays are queued and by this cannot be reused.
                    task.intern.json = writer.WriteAsValue(task);
                }
                tasksJson[n] = task.intern.json.Value;
            }
            tasks.Clear(); // is necessary as tasks List<> may be reused
            syncEvent.tasks = null;
        }
    }
}