// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    public partial class  MonitorStore :  FlioxClient
    {
        internal            string                              hostName;
        
        // --- containers
        public  readonly    EntitySet <JsonKey, HostHits>       hosts;
        public  readonly    EntitySet <JsonKey, ClientHits>     clients;
        public  readonly    EntitySet <JsonKey, UserHits>       users;
        public  readonly    EntitySet <int,     HistoryHits>    histories;

        public MonitorStore(FlioxHub hub, string database = null) : base(hub, database) { }
        
        /// <summary>Reset all request, task and event counters</summary>
        public CommandTask<ClearStatsResult> ClearStats(ClearStats value = null) => SendCommand<ClearStats, ClearStatsResult>(nameof(ClearStats), value);
    }
    
    public class HostHits {
        [Req]   public  JsonKey                         id;
                public  RequestCount                    counts;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class ClientHits {
        [Req]   public  JsonKey                         id;
        [Req]   public  Ref<JsonKey, UserHits>          user;
                public  List<RequestCount>              counts = new List<RequestCount>();
        [Fri.Property (Name =                          "event")]
                public  EventDelivery?                  ev;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public struct EventDelivery {
                public  int                             seq;
                public  int                             queued;
                public  List<string>                    messageSubs;
                public  List<ChangeSubscriptions>       changeSubs;
    }
    
    public sealed class ChangeSubscriptions
    {
        [Req]   public  string                          container;
        [Req]   public  List<Change>                    changes;
                public  string                          filter;
    }
    
    public class UserHits {
        [Req]   public  JsonKey                         id;
        [Req]   public  List<Ref<JsonKey, ClientHits>>  clients;
                public  List<RequestCount>              counts = new List<RequestCount>();
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class HistoryHits {
        [Req]   public  int                             id;
        [Req]   public  int[]                           counters;
                public  int                             lastUpdate;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    
    // --- commands
    public class ClearStats { }
    public class ClearStatsResult { }
}
