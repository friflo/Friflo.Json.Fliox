// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
}
