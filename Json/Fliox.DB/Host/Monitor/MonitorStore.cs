// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    public partial class  MonitorStore :  EntityStore
    {
        public  readonly   EntitySet <JsonKey, ClientInfo>  clients;
        public  readonly   EntitySet <JsonKey, UserInfo>    users;
        public  readonly   string                           monitorName;
        
        public MonitorStore(EntityDatabase database, TypeStore typeStore)
            : base(database, typeStore, null, null)
        {
            monitorName = database.name;
        }
    }
    
    public class ClientInfo {
        [Fri.Required]  public  JsonKey                         id;
        [Fri.Required]  public  Ref<JsonKey, UserInfo>          user;
                        public  List<RequestStats>              stats = new List<RequestStats>();
        [Fri.Property (Name =                                  "event")]  
                        public  EventInfo?                      ev;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public struct EventInfo {
                        public  int                             seq;
                        public  int                             queued;
                        public  List<string>                    messageSubs;
                        public  List<SubscribeChanges>          changeSubs;
    }
    
    public class UserInfo {
        [Fri.Required]  public  JsonKey                         id;
        [Fri.Required]  public  List<Ref<JsonKey, ClientInfo>>  clients;
                        public  List<RequestStats>              stats = new List<RequestStats>();
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
}
