// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    public class AdminStore :  EntityStore
    {
        public  readonly EntitySet <JsonKey, ClientInfo>     clients;
        public  readonly EntitySet <JsonKey, UserInfo>       users;
        
        public AdminStore(EntityDatabase database, TypeStore typeStore, string userId, string clientId) : base(database, typeStore, userId, clientId) {
        }
    }
    
    public class ClientInfo {
        [Fri.Required]  public  JsonKey                                 id;
        [Fri.Required]  public  Ref<JsonKey, UserInfo>                  user;
                        public  int                                     seq;
                        public  int                                     eventQueue;
                        public  Dictionary<string, SubscribeChanges>    changeSubscriptions;
                        public  List<string>                            messageSubscriptions;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class UserInfo {
        [Fri.Required]  public  JsonKey                                 id;
        [Fri.Required]  public  List<Ref<JsonKey, ClientInfo>>          clients;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class AdminDatabase :  EntityDatabase
    {
        private readonly    EntityDatabase      local;
        private readonly    AdminStore          store;
        private readonly    ClientController    dbClientController;
        
        public AdminDatabase (EntityDatabase local, ClientController clientController) : base ("admin") {
            this.local              = local;
            this.dbClientController = clientController;
            store = new AdminStore(local, SyncTypeStore.Get(), "admin", "admin-token");
        }

        public override void Dispose() {
            store.Dispose();
            base.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return local.CreateContainer(name, database);
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            await UpdateStore();
            return await local.ExecuteSync(syncRequest, messageContext);
        }

        private async Task UpdateStore() {
            foreach (var client in dbClientController.clients) {
                if (store.clients.Contains(client))
                    continue;
                var clientInfo = new ClientInfo {
                    id          = client,
                    seq         = 111,
                    eventQueue  = 222
                };
                store.clients.Upsert(clientInfo);
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
