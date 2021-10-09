// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.UserAuth;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    // ReSharper disable UnassignedReadonlyField
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable CollectionNeverUpdated.Global
    public partial class  NodeStore :  EntityStore
    {
        public  readonly   EntitySet <JsonKey, ClientInfo>     clients;
        public  readonly   EntitySet <JsonKey, UserInfo>       users;
        
        public NodeStore(EntityDatabase database, TypeStore typeStore, string userId, string clientId) : base(database, typeStore, userId, clientId) {
        }
    }
    
    public class ClientInfo {
        [Fri.Required]  public  JsonKey                                 id;
        [Fri.Required]  public  Ref<JsonKey, UserInfo>                  user;
                        public  int                                     seq;
                        public  int                                     queuedEvents;
                        public  List<string>                            messageSubs;
                        public  Dictionary<string, SubscribeChanges>    changeSubs;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class UserInfo {
        [Fri.Required]  public  JsonKey                                 id;
        [Fri.Required]  public  List<Ref<JsonKey, ClientInfo>>          clients;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class NodeDatabase :  EntityDatabase
    {
        private readonly    EntityDatabase      nodeDb;
        private readonly    EntityDatabase      db;
        private readonly    NodeStore           store;
        
        public NodeDatabase (EntityDatabase nodeDb, EntityDatabase db) : base ("node") {
            this.nodeDb             = nodeDb;
            this.db                 = db;
            nodeDb.authenticator    = db.authenticator;
            store = new NodeStore(nodeDb, SyncTypeStore.Get(), null, null);
        }

        public override void Dispose() {
            store.Dispose();
            base.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return nodeDb.CreateContainer(name, database);
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            store.UpdateNodeStore(db, syncRequest.userId, syncRequest.token);
            await store.TrySync();
            return await nodeDb.ExecuteSync(syncRequest, messageContext);
        }
    }
    
    public partial class NodeStore {
        internal void UpdateNodeStore(EntityDatabase db, JsonKey userId, string token) {
            SetUser (userId);
            SetToken(token);
            UpdateClients(db);
            UpdateUsers(db);
        }
        
        private void UpdateClients(EntityDatabase db) {
            foreach (var pair in db.clientController.clients) {
                var client = pair.Key;
                if (!clients.TryGet(client, out var clientInfo)) {
                    clientInfo = new ClientInfo { id = client };
                }
                clientInfo.user = pair.Value;
                clients.Upsert(clientInfo);
                var subscriber  = db.eventBroker.GetSubscriber(client);
                var msgSubs     = clientInfo.messageSubs;
                msgSubs?.Clear();
                foreach (var messageSub in subscriber.messageSubscriptions) {
                    if (msgSubs == null) msgSubs = new List<string>();
                    msgSubs.Add(messageSub);
                }
                foreach (var messageSub in subscriber.messagePrefixSubscriptions) {
                    if (msgSubs == null) msgSubs = new List<string>();
                    msgSubs.Add(messageSub + "*");
                }
                clientInfo.messageSubs  = msgSubs;
                clientInfo.seq          = subscriber.Seq;
                clientInfo.queuedEvents = subscriber.EventQueueCount;
                
                clientInfo.changeSubs = subscriber.GetChangeSubscriptions (clientInfo.changeSubs);
            }
        }
        
        private void UpdateUsers(EntityDatabase db) {
            if (!(db.authenticator is UserAuthenticator userAuth))
                return;
            foreach (var user in userAuth.credByUser) {
                if (!users.TryGet(user.Key, out var userInfo)) {
                    userInfo = new UserInfo { id = user.Key };
                }
                if (userInfo.clients == null)
                    userInfo.clients = new List<Ref<JsonKey, ClientInfo>>(user.Value.clients.Count);
                else
                    userInfo.clients.Clear();
                foreach (var client in user.Value.clients) {
                    userInfo.clients.Add(client);
                }
                users.Upsert(userInfo);
            }
        }
    }
}
