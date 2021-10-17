// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host.Stats;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    public partial class  MonitorStore :  EntityStore
    {
        public  readonly    JsonKey                             hostName;
        public  readonly    EntitySet <JsonKey, HostInfo>       hosts;
        public  readonly    EntitySet <JsonKey, ClientInfo>     clients;
        public  readonly    EntitySet <JsonKey, UserInfo>       users;
        public  readonly    EntitySet <int,     HistoryInfo>    histories;

        public MonitorStore(EntityDatabase database, TypeStore typeStore)   : base(database, typeStore, null, null) {
            hostName = new JsonKey(database.HostName);
        }
        public MonitorStore(EntityDatabase database, EntityStore baseStore) : base(database, baseStore) {
            hostName = new JsonKey(baseStore._intern.database.HostName);
        }
        
        public const            string  ClearStats  = nameof(ClearStats); 
    }
    
    public class HostInfo {
        [Fri.Required]  public  JsonKey                         id;
                        public  RequestCount                    counts;
                        
        public override         string                          ToString() => JsonDebug.ToJson(this, false).Replace("\"", "'");
    }
    
    public class ClientInfo {
        [Fri.Required]  public  JsonKey                         id;
        [Fri.Required]  public  Ref<JsonKey, UserInfo>          user;
                        public  List<RequestCount>              counts = new List<RequestCount>();
        [Fri.Property (Name =                                  "event")]  
                        public  EventInfo?                      ev;
                        
        public override         string                          ToString() => JsonDebug.ToJson(this, false).Replace("\"", "'");
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
                        public  List<RequestCount>              counts = new List<RequestCount>();
                        
        public override         string                          ToString() => JsonDebug.ToJson(this, false).Replace("\"", "'");
    }
    
    public class HistoryInfo {
        [Fri.Required]  public  int                             id;
        [Fri.Required]  public  int[]                           counters;
                        public  int                             lastUpdate;
                        
        public override         string                          ToString() => JsonDebug.ToJson(this, false).Replace("\"", "'");
    }
}
