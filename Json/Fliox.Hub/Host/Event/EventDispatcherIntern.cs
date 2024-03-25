// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        internal readonly   Dictionary<ShortString, EventSubClient> subClients;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubClient>             SubClients  => subClients.Values;
        
        /// <summary> Subset of <see cref="subClients"/> eligible for sending events. Either they
        /// are <see cref="EventSubClient.Connected"/> or they <see cref="EventSubClient.queueEvents"/> </summary> 
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<ShortString, EventSubClient> sendClientsMap;
        /// key: database name
        internal readonly   DatabaseSubsMap                         databaseSubsMap;
        //
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<ShortString, EventSubUser>       subUsers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubUser>                   SubUsers    => subUsers.Values;
        
        private  readonly   Dictionary<ShortString, List<ClientDbSubs>> databaseSubsBuffer;
        
        private  readonly   Dictionary<DatabaseSubs, DatabaseSubs>      uniqueDatabaseSubsBuffer;
        
        internal EventDispatcherIntern(EventDispatcher dispatcher) {
            eventDispatcher         = dispatcher;
            monitor                 = new object();
            subClients              = new Dictionary<ShortString, EventSubClient>   (ShortString.Equality);
            sendClientsMap          = new Dictionary<ShortString, EventSubClient>   (ShortString.Equality);
            subUsers                = new Dictionary<ShortString, EventSubUser>     (ShortString.Equality);
            databaseSubsMap         = new DatabaseSubsMap(null);
            databaseSubsBuffer      = new Dictionary<ShortString, List<ClientDbSubs>>(ShortString.Equality); 
            uniqueDatabaseSubsBuffer= new Dictionary<DatabaseSubs, DatabaseSubs>(DatabaseSubs.Equality);
        }
        
        /// <summary> requires lock <see cref="monitor"/> </summary>
        internal void SubscribeChanges (
            in ShortString database,    SubscribeChanges subscribe,     User        user,
            in ShortString clientId,    IEventReceiver   eventReceiver)
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
                subClient   = GetOrCreateSubClient(user, clientId, eventReceiver);
                var subsMap = subClient.databaseSubs;
                if (!subsMap.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs();
                    subsMap.TryAdd(database, databaseSubs);
                }
                databaseSubs.AddChangeSubscription(subscribe);
            }
        }
        
        /// <summary> requires lock <see cref="monitor"/> </summary> 
        internal EventSubClient GetOrCreateSubClient(User user, in ShortString clientId, IEventReceiver eventReceiver) {
            subClients.TryGetValue(clientId, out EventSubClient subClient);
            if (subClient != null) {
                // add to sendClientsMap as the client could have been removed meanwhile caused by a disconnect
                sendClientsMap.TryAdd(subClient.clientId, subClient);
                return subClient;
            }
            if (!subUsers.TryGetValue(user.userId, out var subUser)) {
                var groups = user.GetGroups().Select(group => new ShortString(group));
                subUser = new EventSubUser (user.userId, groups);
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
            in ShortString database,    SubscribeMessage subscribe,     User user,
            in ShortString clientId,    IEventReceiver   eventReceiver)
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
                var subsMap = subClient.databaseSubs;
                if (!subsMap.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs();
                    subsMap.TryAdd(database, databaseSubs);
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
            // --- create / update List<ClientDbSubs> for each database
            foreach (var pair in sendClientsMap) {
                var subClient = pair.Value;
                foreach (var databaseSubPair in subClient.databaseSubs) {
                    var database        = databaseSubPair.Key;
                    var databaseSubs    = databaseSubPair.Value;
                    var clientSub       = new ClientDbSubs(subClient, databaseSubs);
                    if (databaseSubsBuffer.TryGetValue(database, out var list)) {
                        list.Add(clientSub);
                        continue;
                    }
                    databaseSubsBuffer[database] = new List<ClientDbSubs>{ clientSub };
                }
            }
            // --- find unique DatabaseSubs for each database and replace original DatabaseSubs with unique clone
            // This is done for optimization.
            // This enables creating a serialized SyncEvent only once for all clients using the same DatabaseSubs.
            foreach (var pair in databaseSubsBuffer) {
                uniqueDatabaseSubsBuffer.Clear();
                var clientDbSubs = pair.Value;
                for (int n = 0; n < clientDbSubs.Count; n++) {
                    var clientDbSub = clientDbSubs[n];
                    if (uniqueDatabaseSubsBuffer.TryGetValue(clientDbSub.subs, out var equalSubs)) {
                        clientDbSubs[n] = new ClientDbSubs(clientDbSub.client, equalSubs);
                        continue;
                    }
                    var cloneSubs = new DatabaseSubs(clientDbSub.subs);
                    uniqueDatabaseSubsBuffer.Add(cloneSubs, cloneSubs);
                    clientDbSubs[n] = new ClientDbSubs(clientDbSub.client, cloneSubs);
                }
            }
            // --- create an ImmutableArray<ClientDbSubs> from List<ClientDbSubs> for each database
            databaseSubsMap.map.Clear();
            foreach (var pair in databaseSubsBuffer) {
                var subs = pair.Value;
                if (subs.Count == 0) {
                    continue;
                }
                var databaseSubs    = subs.ToArray();
                var database        = pair.Key;
                databaseSubsMap.map.Add(database, databaseSubs);
            }
            // --- create EventSubClient[] containing all clients to send event to
            var clients = new EventSubClient[sendClientsMap.Count];
            var index = 0;
            foreach (var pair in sendClientsMap) {
                clients[index++] = pair.Value;
            }
            eventDispatcher.sendClients = clients.ToArray();
        }
    }
    
    internal readonly struct DatabaseSubsMap
    {
        internal readonly Dictionary<ShortString, ClientDbSubs[]> map;
        
        internal DatabaseSubsMap(object dummy) {
            map = new Dictionary<ShortString, ClientDbSubs[]>(ShortString.Equality);
        }
    }

    internal readonly struct ClientDbSubs
    {
        internal readonly   EventSubClient  client;
        internal readonly   DatabaseSubs    subs;
        
        internal ClientDbSubs(EventSubClient client, DatabaseSubs subs) {
            this.client = client;
            this.subs   = subs;
        }
    }
}