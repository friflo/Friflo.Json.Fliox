// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Accumulator;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Utils;
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
    #region - members
        public              bool                                    SendUserIds         { get; set; } = true;
        public              bool                                    SendClientIds       { get; set; } = false;
        public              ChangeAccumulator                       ChangeAccumulator   { get; set; }
        private  readonly   SharedEnv                               sharedEnv;
        private  readonly   JsonEvaluator                           jsonEvaluator;
        /// <summary>buffer for serialized <see cref="SyncEvent"/>'s to avoid frequent allocations</summary>
        private  readonly   List<JsonValue>                         syncEventBuffer;
        private  readonly   List<JsonValue>                         eventMessageBuffer;
        
    /// START - fields require lock (<see cref="subClients"/>)
        /// key: <see cref="EventSubClient.clientId"/>
        [DebuggerBrowsable(Never)]
        private  readonly   Dictionary<JsonKey, EventSubClient>     subClients;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubClient>             SubClients  => subClients.Values;
        
        /// <summary> Subset of <see cref="subClients"/> eligible for sending events. Either they
        /// are <see cref="EventSubClient.Connected"/> or they <see cref="EventSubClient.queueEvents"/> </summary> 
        [DebuggerBrowsable(Never)]
        private  readonly   Dictionary<JsonKey, EventSubClient>     sendClientsMap;
        /// <summary> Array of <see cref="EventSubClient"/>'s stored in <see cref="sendClientsMap"/><br/>
        /// Is updated whenever <see cref="sendClientsMap"/> is modified. Enables enumerating clients without heap allocation.
        /// This would be the case if sendClientsMap is a <see cref="ConcurrentDictionary{TKey,TValue}"/></summary>
        private             EventSubClient[]                        sendClients = Array.Empty<EventSubClient>();
        private  readonly   Dictionary<SmallString, DatabaseSubs[]> databaseSubsMap;
        //
        [DebuggerBrowsable(Never)]
        private  readonly   Dictionary<JsonKey, EventSubUser>       subUsers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubUser>               SubUsers    => subUsers.Values;
    // END - fields require lock

        /// <summary> exposed only for test assertions. <see cref="EventDispatcher"/> lives on Hub. <br/>
        /// If required its state (subscribed client) can be exposed by <see cref="DB.Monitor.ClientHits"/></summary>
        [DebuggerBrowsable(Never)]
        public              int                                     SubscribedClientsCount => subClients.Count;
        //
        internal readonly   EventDispatching                        dispatching;
        /// <see cref="clientEventLoop"/> and <see cref="clientEventWriter"/>
        /// are used as a queue to send pending <see cref="SyncEvent"/>'s
        private  readonly   Task                                    clientEventLoop;
        private  readonly   IDataChannelWriter<EventSubClient>      clientEventWriter;

        public   override   string                                  ToString() => $"subscribers: {subClients.Count}";

        private const string MissingEventReceiver = "subscribing events requires an eventReceiver. E.g a WebSocket as a target for push events.";
    #endregion

    #region - initialize
        public EventDispatcher (EventDispatching dispatching, SharedEnv env = null) {
            sharedEnv           = env ?? SharedEnv.Default;
            jsonEvaluator       = new JsonEvaluator();
            subClients          = new Dictionary<JsonKey, EventSubClient>   (JsonKey.Equality);
            sendClientsMap      = new Dictionary<JsonKey, EventSubClient>   (JsonKey.Equality);
            subUsers            = new Dictionary<JsonKey, EventSubUser>     (JsonKey.Equality);
            databaseSubsMap     = new Dictionary<SmallString,DatabaseSubs[]>(SmallString.Equality);
            syncEventBuffer     = new List<JsonValue>();
            eventMessageBuffer  = new List<JsonValue>();
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
            lock (subClients) {
                return subClients.TryGetValue(key, out subClient);
            }
        }
        
        /// used for test assertion
        public int QueuedEventsCount() {
            int count = 0;
            lock (subClients) {
                foreach (var pair in subClients) {
                    count += pair.Value.QueuedEventsCount;
                }
                return count;
            }
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
    #endregion
        
    #region - add / remove subscritions
        internal bool SubscribeMessage(
            in SmallString database,    SubscribeMessage subscribe,     User       user,
            in JsonKey     clientId,    EventReceiver    eventReceiver, out string error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            lock (subClients) {
                SubscribeMessageIntern(database, subscribe, user, clientId, eventReceiver);
                UpdateSendClients();
                return true;
            }
        }
        
        /// <summary> requires lock <see cref="subClients"/> </summary>
        private void SubscribeMessageIntern(
            in SmallString database,    SubscribeMessage subscribe,     User user,
            in JsonKey     clientId,    EventReceiver    eventReceiver)
        {
            EventSubClient subClient;
            var remove = subscribe.remove;
            if (remove.HasValue && remove.Value) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    return;
                }
                databaseSubs.RemoveMessageSubscription(subscribe.name);
                RemoveEmptySubClient(subClient);
            } else {
                subClient = GetOrCreateSubClientIntern(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(subClient, database.value);
                    subClient.databaseSubs.TryAdd(database, databaseSubs);
                }
                databaseSubs.AddMessageSubscription(subscribe.name);
            }
        }

        internal bool SubscribeChanges (
            in SmallString database,    SubscribeChanges subscribe,     User        user,
            in JsonKey      clientId,   EventReceiver    eventReceiver, out string  error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            lock (subClients) {
                SubscribeChangesIntern(database, subscribe, user, clientId, eventReceiver);
                UpdateSendClients();
                return true;
            }
        }
        
        /// <summary> requires lock <see cref="subClients"/> </summary>
        private void SubscribeChangesIntern (
            in SmallString database,    SubscribeChanges subscribe,     User        user,
            in JsonKey      clientId,   EventReceiver    eventReceiver)
        {
            EventSubClient subClient;
            if (subscribe.changes.Count == 0) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                    return;
                databaseSubs.RemoveChangeSubscription(subscribe.container);
                RemoveEmptySubClient(subClient);
            } else {
                subClient = GetOrCreateSubClientIntern(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(subClient, database.value);
                    subClient.databaseSubs.TryAdd(database, databaseSubs);
                }
                databaseSubs.AddChangeSubscription(subscribe);
            }
        }
        
        internal EventSubClient GetOrCreateSubClient(User user, in JsonKey clientId, EventReceiver eventReceiver) {
            lock (subClients) {
                var result = GetOrCreateSubClientIntern(user, clientId, eventReceiver);
                UpdateSendClients();
                return result;
            }
        }
        
        /// <summary> requires lock <see cref="subClients"/> </summary> 
        private EventSubClient GetOrCreateSubClientIntern(User user, in JsonKey clientId, EventReceiver eventReceiver) {
            subClients.TryGetValue(clientId, out EventSubClient subClient);
            if (subClient != null) {
                // add to sendClientsMap as the client could have been removed meanwhile caused by a disconnect
                sendClientsMap.TryAdd(subClient.clientId, subClient);
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
            subClients.    TryAdd(clientId, subClient);
            sendClientsMap.TryAdd(clientId, subClient);
            subUser.clients.TryAdd(subClient, true);
            return subClient;
        }

        /// <summary>
        /// Don't remove empty subClient as the state of <see cref="EventSubClient.eventCounter"/> need to be preserved.
        /// </summary>
        // ReSharper disable once UnusedParameter.Local
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
        
        private readonly Dictionary<string, List<DatabaseSubs>> dbSubsMap = new Dictionary<string, List<DatabaseSubs>>(); 
        
        /// <summary> requires lock <see cref="subClients"/> </summary> 
        private void UpdateSendClients()
        {
            foreach (var pair in dbSubsMap) {
                pair.Value.Clear();
            }
            foreach (var pair in sendClientsMap) {
                var subClient = pair.Value;
                foreach (var databaseSubPair in subClient.databaseSubs) {
                    var databaseSubs = databaseSubPair.Value;
                    if (dbSubsMap.TryGetValue(databaseSubs.database, out var list)) {
                        list.Add(databaseSubs);
                        continue;
                    }
                    dbSubsMap[databaseSubs.database] = new List<DatabaseSubs>{ databaseSubs };
                }
            }
            databaseSubsMap.Clear();
            foreach (var pair in dbSubsMap) {
                var subs = pair.Value;
                if (subs.Count == 0) {
                    continue;
                }
                var databaseSubs    = subs.ToArray();
                var database        = pair.Key;
                databaseSubsMap.Add(new SmallString(database), databaseSubs);
            }
            var clients = new EventSubClient[sendClientsMap.Count];
            var index = 0;
            foreach (var pair in sendClientsMap) {
                clients[index++] = pair.Value;
            }
            sendClients = clients;
        }
        
        internal void UpdateSubUserGroups(in JsonKey userId, IReadOnlyCollection<String> groups) {
            EventSubUser subUser;
            lock (subClients) {
                if (!subUsers.TryGetValue(userId, out subUser))
                    return;
            }
            subUser.groups.Clear();
            if (groups != null) {
                subUser.groups.UnionWith(groups);
            }
        }
    #endregion

    #region - event distribution
        public void SendQueuedEvents() {
            if (dispatching == EventDispatching.QueueSend) {
                throw new InvalidOperationException($"must not be called if using {nameof(EventDispatcher)}.{EventDispatching.QueueSend}");
            }
            using (var pooleMapper = sharedEnv.Pool.ObjectMapper.Get()) {
                var writer = pooleMapper.instance.writer;
                foreach (var subClient in sendClients) {
                    subClient.SendEvents(writer, eventMessageBuffer, syncEventBuffer);
                }
            }
        }
        
        /// <summary>use single lock to retrieve <paramref name="subClient"/> and <paramref name="databaseSubsArray"/></summary>
        private void GetSubClientAndDatabaseSubs(
                SyncContext     syncContext,
            out EventSubClient  subClient,
            out DatabaseSubs[]  databaseSubsArray)
        {
            JsonKey  clientId = syncContext.clientId;
            lock (subClients) {
                if (clientId.IsNull()) {
                    subClient = null;
                } else {
                    subClients.TryGetValue(clientId, out subClient);
                }
                databaseSubsMap.TryGetValue(syncContext.databaseName, out databaseSubsArray);
            }
        }
        
        private void ProcessSubscriber(EventSubClient subClient, SyncRequest syncRequest, SyncContext syncContext) {
            var eventReceiver = syncContext.eventReceiver;
            if (eventReceiver != null && eventReceiver.IsRemoteTarget()) {
                if (subClient.UpdateTarget (eventReceiver)) {
                    // remote client is using a new connection (WebSocket) so add to sendClients again
                    lock (subClients) {
                        sendClientsMap.TryAdd(subClient.clientId, subClient);
                        UpdateSendClients();
                    }
                }
            }
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            if (!syncContext.authState.hubPermission.queueEvents)
                 return;
            int value =  eventAck.Value;
            subClient.AcknowledgeEventMessages(value);
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
        
        private static bool AccumulateTasks(
            ChangeAccumulator       accumulator,
            EntityDatabase          database,
            List<SyncRequestTask>   syncTasks)
        {
            var processed = true;
            foreach (var syncTask in syncTasks) {
                processed = accumulator.AddSyncTask(database, syncTask) && processed;
            }
            return processed;
        }
        
        /// <summary>
        /// Create serialized <see cref="SyncEvent"/>'s for the passed <see cref="SyncRequest.tasks"/> for
        /// all <see cref="EventSubClient"/>'s having matching <see cref="DatabaseSubs"/>
        /// </summary>
        internal void EnqueueSyncTasks (SyncRequest syncRequest, SyncContext syncContext)
        {
            GetSubClientAndDatabaseSubs(syncContext, out var subClient, out var databaseSubsArray);
            if (subClient != null) {
                ProcessSubscriber (subClient, syncRequest, syncContext);
            }
            if (databaseSubsArray == null) {
                return;
            }
            var syncTasks = syncRequest.tasks;
            if (!HasSubscribableTask(syncTasks)) {
                return; // early out
            }
            var accumulator = ChangeAccumulator;
            if (accumulator != null) {
                if (AccumulateTasks(accumulator, syncContext.Database, syncTasks)) {
                    return;
                }
            }
            var memoryBuffer    = syncContext.MemoryBuffer;
            var database        = syncContext.databaseName;
            foreach (var task in syncTasks) { task.intern.json = null; }
            // reused syncEvent to create a serialized SyncEvent for every EventSubClient
            var syncEvent       = new SyncEvent {
                db                  = database.value,
                tasks               = syncContext.syncBuffers.eventTasks,
                tasksJson           = syncContext.syncBuffers.tasksJson
            };
            if (SendUserIds) {
                syncEvent.usr    = syncRequest.userId;
            }
            using (var pooled = syncContext.ObjectMapper.Get()) {
                ObjectWriter writer     = pooled.instance.writer;
                writer.Pretty           = false;    // write sub's as one liner
                writer.WriteNullMembers = false;
                
                foreach (var databaseSubs in databaseSubsArray) {
                    var client = databaseSubs.client;
                    if (!client.queueEvents && !client.Connected) {
                        lock (subClients) {
                            sendClientsMap.Remove(client.clientId);
                            UpdateSendClients();
                        }
                        continue;
                    }
                    if (!databaseSubs.CreateEventTasks(syncTasks, client, ref syncEvent.tasks, jsonEvaluator)) {
                        continue;
                    }
                    SerializeEventTasks(syncEvent.tasks, ref syncEvent.tasksJson, writer, memoryBuffer);
                    bool sendClientId       = SendClientIds || syncContext.clientId.IsEqual(client.clientId);
                    syncEvent.clt           = sendClientId ? syncContext.clientId : default;
                    JsonValue rawSyncEvent  = RemoteUtils.SerializeSyncEvent(syncEvent, writer);
                    client.EnqueueEvent(rawSyncEvent);
                }
            }
            // clear cached serialized tasks -> enable GC collect byte[]'s
            foreach (var task in syncTasks) { task.intern.json = null; }
        }
        
        /// <summary>Serialize the passed <paramref name="tasks"/> to <paramref name="tasksJson"/></summary>
        /// <remarks>
        /// Optimization:<br/>
        /// - serialize a task only once for multiple targets<br/>
        /// - store only a single byte[] for a task instead of a complex SyncRequestTask which is not used anymore<br/>
        /// </remarks>
        private static void SerializeEventTasks(
                List<SyncRequestTask>   tasks,
            ref List<JsonValue>         tasksJson,
                ObjectWriter            writer,
                MemoryBuffer            memoryBuffer)
        {
            if (tasksJson == null) {
                tasksJson = new List<JsonValue>();
            }
            tasksJson.Clear();
            foreach (var task in tasks) {
                if (task.intern.json == null) {
                    var serializedTask  = new JsonValue(writer.WriteAsBytes(task));
                    serializedTask      = memoryBuffer.Add(serializedTask); // avoid byte[] allocation
                    task.intern.json    = serializedTask;
                }
                tasksJson.Add(task.intern.json.Value);
            }
            tasks.Clear(); // is necessary as tasks List<> may be reused
        }
    #endregion
        
    #region - misc
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
                    await SendEventLoop(clientEventReader, mapper.writer).ConfigureAwait(false);
                }
            } catch (Exception e) {
                var message = "RunSendEventLoop() failed";
                sharedEnv.Logger.Log(HubLog.Error, message, e);
                Debug.Fail(message, e.Message);
            }
        }
        
        private async Task SendEventLoop(IDataChannelReader<EventSubClient> clientEventReader, ObjectWriter writer) {
            var logger  = sharedEnv.Logger;
            while (true) {
                var client = await clientEventReader.ReadAsync().ConfigureAwait(false);
                if (client != null) {
                    client.SendEvents(writer, eventMessageBuffer, syncEventBuffer);
                    continue;
                }
                logger.Log(HubLog.Info, $"ClientEventLoop() returns");
                return;
            }
        }
    #endregion
    }
}