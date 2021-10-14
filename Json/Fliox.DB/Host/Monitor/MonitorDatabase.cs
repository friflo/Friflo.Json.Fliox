// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    public class MonitorDatabase : EntityDatabase
    {
        private  readonly    EntityDatabase      monitorDb;
        internal readonly    EntityDatabase      db;
        private  readonly    MonitorStore        store;
        
        public const string Name = "monitor";
        
        public MonitorDatabase (EntityDatabase monitorDb, EntityDatabase db) {
            monitorDb.ExtensionName = Name;
            this.monitorDb          = monitorDb;
            this.db                 = db;
            monitorDb.Authenticator = db.Authenticator;
            monitorDb.TaskHandler   = new MonitorHandler(this);
            store                   = new MonitorStore(monitorDb, HostTypeStore.Get());
            db.extensionDbs.Add(monitorDb.ExtensionName, this);
        }

        public override void Dispose() {
            store.Dispose();
            base.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return monitorDb.CreateContainer(name, database);
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            if (FindReadEntities(nameof(MonitorStore.clients), syncRequest.tasks)) {
                store.UpdateClients(db);
            }
            if (FindReadEntities(nameof(MonitorStore.users), syncRequest.tasks)) {
                store.UpdateUsers(db);
            }
            store.SetUser (syncRequest.userId);
            store.SetToken(syncRequest.token);
            await store.TrySync().ConfigureAwait(false);
            messageContext.customData = monitorDb.TaskHandler;
            return await monitorDb.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
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
        internal void UpdateClients(EntityDatabase db) {
            foreach (var pair in db.ClientController.Clients) {
                UserClient client   = pair.Value;
                var clientId        = pair.Key;
                clients.TryGet(clientId, out var clientInfo);
                if (clientInfo == null) {
                    clientInfo = new ClientInfo { id = clientId };
                }
                clientInfo.user     = client.userId;
                RequestStats.StatsToList(clientInfo.stats, client.stats, MonitorDatabase.Name);
                clientInfo.ev       = GetEventInfo(db, clientInfo);

                clients.Upsert(clientInfo);
            }
        }
        
        private static EventInfo? GetEventInfo (EntityDatabase db, ClientInfo clientInfo) {
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
        
        internal void UpdateUsers(EntityDatabase db) {
            foreach (var pair in db.Authenticator.users) {
                if (!users.TryGet(pair.Key, out var userInfo)) {
                    userInfo = new UserInfo { id = pair.Key };
                }
                User user   = pair.Value;
                RequestStats.StatsToList(userInfo.stats, user.stats, MonitorDatabase.Name);

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
    }
}
