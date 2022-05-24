// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;

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
        private  readonly   FlioxHub            monitorHub;
        private  readonly   FlioxHub            hub;

        public   override   string              ToString()  => name;
        public   override   string              StorageName => stateDB.StorageName;

        public MonitorDB (string name, FlioxHub hub, DbOpt opt = null)
            : base (name, new MonitorHandler(hub), opt)
        {
            this.hub        = hub  ?? throw new ArgumentNullException(nameof(hub));
            var typeSchema  = NativeTypeSchema.Create(typeof(MonitorStore));
            Schema          = new DatabaseSchema(typeSchema);
            stateDB         = new MemoryDatabase(name, null, MemoryType.NonConcurrent);
            monitorHub      = new FlioxHub(stateDB, hub.sharedEnv);
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return stateDB.CreateContainer(name, database);
        }

        public override async Task ExecuteSyncPrepare(SyncRequest syncRequest, ExecuteContext executeContext) {
            var pool = executeContext.pool;
            using (var pooled  = pool.Type(() => new MonitorStore(monitorHub)).Get()) {
                var monitor = pooled.instance;
                monitor.hostName = hub.hostName;
                var tasks = syncRequest.tasks;
                if (FindTask(nameof(MonitorStore.clients),  tasks)) monitor.UpdateClients  (hub, name);
                if (FindTask(nameof(MonitorStore.users),    tasks)) monitor.UpdateUsers    (hub.Authenticator, name);
                if (FindTask(nameof(MonitorStore.histories),tasks)) monitor.UpdateHistories(hub.hostStats.requestHistories);
                if (FindTask(nameof(MonitorStore.hosts),    tasks)) monitor.UpdateHost     (hub.hostStats);
                
                await monitor.TrySyncTasks().ConfigureAwait(false);
            }
        }
        
        private static bool FindTask(string container, List<SyncRequestTask> tasks) {
            foreach (var task in tasks) {
                if (task is ReadEntities read && read.container == container)
                    return true;
                if (task is QueryEntities query && query.container == container)
                    return true;
            }
            return false;
        }
    }
    
    public partial class MonitorStore
    {
        internal void UpdateClients(FlioxHub hub, string monitorName) {
            foreach (var pair in hub.ClientController.Clients) {
                UserClient client   = pair.Value;
                var clientId        = pair.Key;
                clients.TryGet(clientId, out var clientHits);
                if (clientHits == null) {
                    clientHits = new ClientHits { id = clientId };
                }
                clientHits.user     = client.userId;
                RequestCount.CountsToList(clientHits.counts, client.requestCounts, monitorName);
                clientHits.ev       = GetEventDelivery(hub, clientHits);

                clients.Upsert(clientHits);
            }
        }
        
        private static EventDelivery? GetEventDelivery (FlioxHub hub, ClientHits clientHits) {
            if (hub.EventDispatcher == null)
                return null;
            if (!hub.EventDispatcher.TryGetSubscriber(clientHits.id, out var subscriber)) {
                return null;
            }
            var msgSubs     = clientHits.ev?.messageSubs;
            msgSubs?.Clear();
            foreach (var messageSub in subscriber.messageSubscriptions) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub);
            }
            foreach (var messageSub in subscriber.messagePrefixSubscriptions) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub + "*");
            }
            var changeSubs  = subscriber.GetChangeSubscriptions (clientHits.ev?.changeSubs);
            return new EventDelivery {
                seq         = subscriber.Seq,
                queued      = subscriber.EventQueueCount,
                messageSubs = msgSubs,
                changeSubs  = changeSubs
            };
        }
        
        internal void UpdateUsers(Authenticator authenticator, string monitorName) {
            foreach (var pair in authenticator.users) {
                if (!users.TryGet(pair.Key, out var userHits)) {
                    userHits = new UserHits { id = pair.Key };
                }
                User user   = pair.Value;
                RequestCount.CountsToList(userHits.counts, user.requestCounts, monitorName);

                var userClients = user.clients;
                if (userHits.clients == null) {
                    userHits.clients = new List<Ref<JsonKey, ClientHits>>(userClients.Count);
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
                if (!histories.TryGet(history.resolution, out var historyHits)) {
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
        
        internal void UpdateHost(HostStats hostStats) {
            var hostNameKey = new JsonKey(hostName);
            if (!hosts.TryGet(hostNameKey, out var hostHits)) {
                hostHits = new HostHits { id = hostNameKey };
            }
            hostHits.counts = hostStats.requestCount;
            hosts.Upsert(hostHits);
        }
    }
}
