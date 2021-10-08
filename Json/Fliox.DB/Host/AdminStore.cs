// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    // ReSharper disable UnassignedReadonlyField
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable CollectionNeverUpdated.Global
    public class AdminStore :  EntityStore
    {
        public  readonly   EntitySet <JsonKey, ClientInfo>     clients;
        public  readonly   EntitySet <JsonKey, UserInfo>       users;
        
        public AdminStore(EntityDatabase database, TypeStore typeStore, string userId, string clientId) : base(database, typeStore, userId, clientId) {
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
    
    public class AdminDatabase :  EntityDatabase
    {
        private readonly    EntityDatabase      adminDb;
        private readonly    EntityDatabase      defaultDb;
        private readonly    AdminStore          store;
        
        public AdminDatabase (EntityDatabase adminDb, EntityDatabase defaultDb) : base ("admin") {
            this.adminDb    = adminDb;
            this.defaultDb  = defaultDb;
            store           = new AdminStore(adminDb, SyncTypeStore.Get(), "admin", "admin-token");
        }

        public override void Dispose() {
            store.Dispose();
            base.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return adminDb.CreateContainer(name, database);
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            await UpdateStore();
            return await adminDb.ExecuteSync(syncRequest, messageContext);
        }

        private async Task UpdateStore() {
            foreach (var client in defaultDb.clientController.clients) {
                if (!store.clients.TryGet(client, out var clientInfo)) {
                    clientInfo = new ClientInfo { id          = client };
                }
                store.clients.Upsert(clientInfo);
                var subscriber  = defaultDb.eventBroker.GetSubscriber(client);
                var msgSubs     = clientInfo.messageSubs;
                msgSubs?.Clear();
                foreach (var messageSub in subscriber.messageSubscriptions) {
                    if (msgSubs == null)
                        msgSubs = new List<string>();
                    msgSubs.Add(messageSub);
                }
                foreach (var messageSub in subscriber.messagePrefixSubscriptions) {
                    if (msgSubs == null)
                        msgSubs = new List<string>();
                    msgSubs.Add(messageSub + "*");
                }
                clientInfo.messageSubs  = msgSubs;
                clientInfo.seq          = subscriber.Seq;
                clientInfo.queuedEvents = subscriber.EventQueueCount;
                
                clientInfo.changeSubs = subscriber.GetChangeSubscriptions (clientInfo.changeSubs);
            }
            /* 
            var user1 = new UserInfo {
                id          = new JsonKey("user-1"),
                clients     = new List<Ref<JsonKey, ClientInfo>> { new JsonKey("some-client") },
            }; */

            await store.Sync();
        }
    }
}
