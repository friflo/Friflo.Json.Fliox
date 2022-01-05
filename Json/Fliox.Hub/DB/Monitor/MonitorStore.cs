// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    public partial class  MonitorStore :  FlioxClient
    {
        internal            string                              hostName;
        
        public  readonly    EntitySet <JsonKey, HostInfo>       hosts;
        public  readonly    EntitySet <JsonKey, ClientInfo>     clients;
        public  readonly    EntitySet <JsonKey, UserInfo>       users;
        public  readonly    EntitySet <int,     HistoryInfo>    histories;

        public MonitorStore(FlioxHub hub, string database = null) : base(hub, database) { }
        
        public CommandTask<ClearStatsResult> ClearStats(ClearStats value = null) => SendCommand<ClearStats, ClearStatsResult>(nameof(ClearStats), value);
    }
    
    public class HostInfo {
        [Fri.Required]  public  JsonKey                         id;
                        public  RequestCount                    counts;
                        
        public override         string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class ClientInfo {
        [Fri.Required]  public  JsonKey                         id;
        [Fri.Required]  public  Ref<JsonKey, UserInfo>          user;
                        public  List<RequestCount>              counts = new List<RequestCount>();
        [Fri.Property (Name =                                  "event")]  
                        public  EventInfo?                      ev;
                        
        public override         string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public struct EventInfo {
                        public  int                             seq;
                        public  int                             queued;
                        public  List<string>                    messageSubs;
                        public  List<ChangeSubscriptions>       changeSubs;
    }
    
    public sealed class ChangeSubscriptions
    {
        [Fri.Required]  public  string                          container;
        [Fri.Required]  public  List<Change>                    changes;
                        public  string                          filter;
    }
    
    public class UserInfo {
        [Fri.Required]  public  JsonKey                         id;
        [Fri.Required]  public  List<Ref<JsonKey, ClientInfo>>  clients;
                        public  List<RequestCount>              counts = new List<RequestCount>();
                        
        public override         string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class HistoryInfo {
        [Fri.Required]  public  int                             id;
        [Fri.Required]  public  int[]                           counters;
                        public  int                             lastUpdate;
                        
        public override         string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    
    // --- commands
    public class ClearStats { }
    public class ClearStatsResult { }
}
