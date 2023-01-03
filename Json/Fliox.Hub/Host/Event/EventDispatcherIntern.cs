// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// Contains <see cref="EventDispatcher"/> fields which require a lock(<see cref="monitor"/>) when accessing     
    /// </summary>
    internal readonly struct EventDispatcherIntern
    {
        private  readonly   EventDispatcher                         eventDispatcher;
        internal readonly   object                                  monitor;
        /// key: <see cref="EventSubClient.clientId"/>
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<JsonKey, EventSubClient>     subClients;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubClient>             SubClients  => subClients.Values;
        
        /// <summary> Subset of <see cref="subClients"/> eligible for sending events. Either they
        /// are <see cref="EventSubClient.Connected"/> or they <see cref="EventSubClient.queueEvents"/> </summary> 
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<JsonKey, EventSubClient>     sendClientsMap;
        /// key: database name
        internal readonly   Dictionary<SmallString, DatabaseSubs[]> databaseSubsMap;
        //
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<JsonKey, EventSubUser>       subUsers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubUser>               SubUsers    => subUsers.Values;
        
        private  readonly   Dictionary<string, List<DatabaseSubs>>  databaseSubsBuffer;
        
        internal EventDispatcherIntern(EventDispatcher dispatcher) {
            eventDispatcher     = dispatcher;
            monitor             = new object();
            subClients          = new Dictionary<JsonKey, EventSubClient>   (JsonKey.Equality);
            sendClientsMap      = new Dictionary<JsonKey, EventSubClient>   (JsonKey.Equality);
            subUsers            = new Dictionary<JsonKey, EventSubUser>     (JsonKey.Equality);
            databaseSubsMap     = new Dictionary<SmallString,DatabaseSubs[]>(SmallString.Equality);
            databaseSubsBuffer  = new Dictionary<string, List<DatabaseSubs>>(); 
        }
        
        /// <summary> requires lock <see cref="monitor"/> </summary>
        internal void SubscribeChanges (
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
                subClient = GetOrCreateSubClient(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(subClient, database.value);
                    subClient.databaseSubs.TryAdd(database, databaseSubs);
                }
                databaseSubs.AddChangeSubscription(subscribe);
            }
        }
        
        /// <summary> requires lock <see cref="monitor"/> </summary> 
        internal EventSubClient GetOrCreateSubClient(User user, in JsonKey clientId, EventReceiver eventReceiver) {
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
            var dispatcher = eventDispatcher.dispatching == EventDispatching.QueueSend ? eventDispatcher : null;
            subClient = new EventSubClient(eventDispatcher.sharedEnv, subUser, clientId, dispatcher);
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
        
        /// <summary> requires lock <see cref="monitor"/> </summary>
        internal void SubscribeMessage(
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
                subClient = GetOrCreateSubClient(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(subClient, database.value);
                    subClient.databaseSubs.TryAdd(database, databaseSubs);
                }
                databaseSubs.AddMessageSubscription(subscribe.name);
            }
        }

        /// <summary> requires lock <see cref="monitor"/> </summary> 
        internal void UpdateSendClients()
        {
            foreach (var pair in databaseSubsBuffer) {
                pair.Value.Clear();
            }
            foreach (var pair in sendClientsMap) {
                var subClient = pair.Value;
                foreach (var databaseSubPair in subClient.databaseSubs) {
                    var databaseSubs = databaseSubPair.Value;
                    if (databaseSubsBuffer.TryGetValue(databaseSubs.database, out var list)) {
                        list.Add(databaseSubs);
                        continue;
                    }
                    databaseSubsBuffer[databaseSubs.database] = new List<DatabaseSubs>{ databaseSubs };
                }
            }
            databaseSubsMap.Clear();
            foreach (var pair in databaseSubsBuffer) {
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
            eventDispatcher.sendClients = clients;
        }
    }
}