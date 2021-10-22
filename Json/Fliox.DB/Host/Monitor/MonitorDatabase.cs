// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Host.Stats;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    public class MonitorDatabase : EntityDatabase
    {
        internal readonly    EntityDatabase stateDB;
        private  readonly    MonitorStore   stateStore;
        
        public const string Name = "monitor";
        
        public MonitorDatabase (DatabaseHub extensionBase, DbOpt opt = null)
            : base (extensionBase, Name, new DbOpt(taskHandler: new MonitorHandler())) {
            stateDB         = new MemoryDatabase();
            var monitorHub  = new DatabaseHub(stateDB);
            ((MonitorHandler)taskHandler).monitorDB = this;
            stateStore  = new MonitorStore(extensionBase.hostName, monitorHub, HostTypeStore.Get());
        }

        public override void Dispose() {
            stateStore.Dispose();
            base.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return stateDB.CreateContainer(name, database);
        }

        public override async Task ExecuteSyncPrepare(SyncRequest syncRequest, MessageContext messageContext) {
            var tasks = syncRequest.tasks;
            if (FindReadEntities(nameof(MonitorStore.clients),  tasks)) stateStore.UpdateClients  (hub, name);
            if (FindReadEntities(nameof(MonitorStore.users),    tasks)) stateStore.UpdateUsers    (hub.Authenticator, name);
            if (FindReadEntities(nameof(MonitorStore.histories),tasks)) stateStore.UpdateHistories(hub.hostStats.requestHistories);
            if (FindReadEntities(nameof(MonitorStore.hosts),    tasks)) stateStore.UpdateHost     (hub.hostStats);
            
            await stateStore.TryExecuteTasksAsync().ConfigureAwait(false);
        }
        
        private static bool FindReadEntities(string container, List<SyncRequestTask> tasks) {
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
        internal void UpdateClients(DatabaseHub db, string monitorName) {
            foreach (var pair in db.ClientController.Clients) {
                UserClient client   = pair.Value;
                var clientId        = pair.Key;
                clients.TryGet(clientId, out var clientInfo);
                if (clientInfo == null) {
                    clientInfo = new ClientInfo { id = clientId };
                }
                clientInfo.user     = client.userId;
                RequestCount.CountsToList(clientInfo.counts, client.requestCounts, monitorName);
                clientInfo.ev       = GetEventInfo(db, clientInfo);

                clients.Upsert(clientInfo);
            }
        }
        
        private static EventInfo? GetEventInfo (DatabaseHub db, ClientInfo clientInfo) {
            if (db.EventBroker == null)
                return null;
            if (!db.EventBroker.TryGetSubscriber(clientInfo.id, out var subscriber)) {
                return null;
            }
            var msgSubs     = clientInfo.ev?.messageSubs;
            msgSubs?.Clear();
            foreach (var messageSub in subscriber.messageSubscriptions) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub);
            }
            foreach (var messageSub in subscriber.messagePrefixSubscriptions) {
                if (msgSubs == null) msgSubs = new List<string>();
                msgSubs.Add(messageSub + "*");
            }
            var changeSubs  = subscriber.GetChangeSubscriptions (clientInfo.ev?.changeSubs);
            return new EventInfo {
                seq         = subscriber.Seq,
                queued      = subscriber.EventQueueCount,
                messageSubs = msgSubs,
                changeSubs  = changeSubs
            };
        }
        
        internal void UpdateUsers(Authenticator authenticator, string monitorName) {
            foreach (var pair in authenticator.users) {
                if (!users.TryGet(pair.Key, out var userInfo)) {
                    userInfo = new UserInfo { id = pair.Key };
                }
                User user   = pair.Value;
                RequestCount.CountsToList(userInfo.counts, user.requestCounts, monitorName);

                var userClients = user.clients;
                if (userInfo.clients == null) {
                    userInfo.clients = new List<Ref<JsonKey, ClientInfo>>(userClients.Count);
                } else {
                    userInfo.clients.Clear();
                }
                foreach (var client in userClients) {
                    userInfo.clients.Add(client);
                }
                users.Upsert(userInfo);
            }
        }
        
        internal void UpdateHistories(RequestHistories requestHistories) {
            foreach (var history in requestHistories.histories) {
                if (!histories.TryGet(history.resolution, out var historyInfo)) {
                    historyInfo = new HistoryInfo {
                        id          = history.resolution,
                        counters    = new int[history.Length]
                    };
                }
                history.CopyCounters(historyInfo.counters);
                historyInfo.lastUpdate  = history.LastUpdate;
                histories.Upsert(historyInfo);
            }
        }
        
        internal void UpdateHost(HostStats hostStats) {
            if (!hosts.TryGet(hostName, out var hostInfo)) {
                hostInfo = new HostInfo { id = hostName };
            }
            hostInfo.counts = hostStats.requestCount;
            hosts.Upsert(hostInfo);
        }
    }
}
