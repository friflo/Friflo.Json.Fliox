// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    /// <summary>
    /// <see cref="MonitorDB"/> store access information of the Hub and its databases:<br/>
    /// - request and task count executed per user <br/>
    /// - request and task count executed per client. A user can access without, one or multiple client ids. <br/>
    /// - events sent to (or buffered for) clients subscribed by these clients. <br/>
    /// - aggregated access counts of the Hub in the last 30 seconds and 30 minutes.
    /// </summary>
    public sealed class MonitorDB : EntityDatabase
    {
        // --- private / internal
        internal readonly   EntityDatabase      stateDB;
        internal readonly   FlioxHub            monitorHub;

        public   override   string              StorageType => stateDB.StorageType;
        
        private static readonly DatabaseSchema MonitorSchema = DatabaseSchema.CreateFromType(typeof(MonitorStore));

        public MonitorDB (string dbName, FlioxHub hub)
            : base (dbName, MonitorSchema, new MonitorService(hub))
        {
            hub.MonitorAccess = MonitorAccess.All;
            ((MonitorService)service).monitorDB = this;
            stateDB         = new MemoryDatabase(dbName) { ContainerType = MemoryType.NonConcurrent };
            monitorHub      = new FlioxHub(stateDB, hub.sharedEnv);
        }

        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return stateDB.CreateContainer(name, database);
        }

        internal static bool FindTask(string container, ListOne<SyncRequestTask> tasks) {
            var containerName = new ShortString(container);
            foreach (var task in tasks.GetReadOnlySpan()) {
                if (task is ReadEntities read && read.container.IsEqual(containerName))
                    return true;
                if (task is QueryEntities query && query.container.IsEqual(containerName))
                    return true;
            }
            return false;
        }
    }
    
    public partial class MonitorStore
    {
        internal void UpdateClients(FlioxHub hub, string monitorName) {
            foreach (var pair in hub.ClientController.clients) {
                UserClient client   = pair.Value;
                var clientId        = pair.Key;
                clients.Local.TryGetEntity(clientId, out var clientHits);
                if (clientHits == null) {
                    clientHits = new ClientHits { id = clientId };
                }
                clientHits.user     = client.userId;
                ClusterUtils.CountsMapToList(clientHits.counts, client.requestCounts, monitorName);
                clientHits.subscriptionEvents       = GetSubscriptionEvents(hub, clientHits);

                clients.Upsert(clientHits);
            }
        }
        
        private static SubscriptionEvents? GetSubscriptionEvents (FlioxHub hub, ClientHits clientHits) {
            var dispatcher = hub.EventDispatcher;
            if (dispatcher == null)
                return null;
            if (!dispatcher.TryGetSubscriber(clientHits.id, out var subscriber)) {
                return null;
            }
            return ClusterUtils.GetSubscriptionEvents(dispatcher, subscriber, clientHits.subscriptionEvents);
        }
        
        internal void UpdateUsers(Authenticator authenticator, string monitorName) {
            foreach (var pair in authenticator.users) {
                if (!users.Local.TryGetEntity(pair.Key, out var userHits)) {
                    userHits = new UserHits { id = pair.Key };
                }
                User user   = pair.Value;
                ClusterUtils.CountsMapToList(userHits.counts, user.requestCounts, monitorName);

                var userClients = user.clients;
                if (userHits.clients == null) {
                    userHits.clients = new List<ShortString>(userClients.Count);
                } else {
                    userHits.clients.Clear();
                }
                foreach (var clientPair in userClients) {
                    userHits.clients.Add(clientPair.Key);
                }
                users.Upsert(userHits);
            }
        }
        
        internal void UpdateHistories(RequestHistories requestHistories) {
            foreach (var history in requestHistories.histories) {
                if (!histories.Local.TryGetEntity(history.resolution, out var historyHits)) {
                    historyHits = new HistoryHits {
                        id          = history.resolution,
                        counters    = new int[history.Length]
                    };
                }
                history.CopyCounters(historyHits.counters);
                historyHits.lastUpdate  = history.LastUpdate;
                histories.Upsert(historyHits);
            }
        }
        
        internal void UpdateHost(FlioxHub hub, HostStats hostStats) {
            var name = new ShortString(hub.HostName);
            HostHits hostHits;
            if (hosts.Local.Count == 0) {
                hostHits = new HostHits { id = name };
            } else {
                hostHits = hosts.Local[name];
            }
            hostHits.counts = hostStats.requestCount;
            hosts.Upsert(hostHits);
        }
    }
}
