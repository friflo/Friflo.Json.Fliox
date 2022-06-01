// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

using Req = Friflo.Json.Fliox.Mapper.RequiredMemberAttribute;
using Property = Friflo.Json.Fliox.Mapper.PropertyMemberAttribute;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    /// <summary>
    /// <see cref="MonitorStore"/> expose access information of the Hub and its databases:<br/>
    /// - request and task count executed per user <br/>
    /// - request and task count executed per client. A user can access without, one or multiple client ids. <br/>
    /// - events sent to (or buffered for) clients subscribed by these clients. <br/>
    /// - aggregated access counts of the Hub in the last 30 seconds and 30 minutes.
    /// </summary>
    public partial class  MonitorStore :  FlioxClient
    {
        internal            string                              hostName;
        
        // --- containers
        public  readonly    EntitySet <JsonKey, HostHits>       hosts;
        public  readonly    EntitySet <JsonKey, UserHits>       users;
        public  readonly    EntitySet <JsonKey, ClientHits>     clients;
        public  readonly    EntitySet <int,     HistoryHits>    histories;

        public MonitorStore(FlioxHub hub, string database = null) : base(hub, database) { }
        
        /// <summary>Reset all request, task and event counters</summary>
        public CommandTask<ClearStatsResult> ClearStats(ClearStats value = null) => SendCommand<ClearStats, ClearStatsResult>(nameof(ClearStats), value);
    }
    
    /// <summary>number of requests and tasks executed by the host. Container contains always a single record</summary>
    public sealed class HostHits {
        /// <summary>host name</summary>
        [Req]   public  JsonKey                         id;
        /// <summary>number of executed requests and tasks per database</summary>
                public  RequestCount                    counts;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>all user clients and number of executed user requests and tasks</summary>
    public sealed class UserHits {
        /// <summary>user id </summary>
        [Req]   public  JsonKey                         id;
        /// <summary>list of clients owned by a user</summary>
        [Req]   public  List<Ref<JsonKey, ClientHits>>  clients;
        /// <summary>number executed requests and tasks per database</summary>
        public  List<RequestCount>                      counts = new List<RequestCount>();
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>information about requests, tasks, events and subscriptions of a client</summary>
    public sealed class ClientHits {
        /// <summary>client id </summary>
        [Req]   public  JsonKey                         id;
        /// <summary>user owning the client</summary>
        [Req]   public  Ref<JsonKey, UserHits>          user;
        /// <summary>number executed requests and tasks per database</summary>
                public  List<RequestCount>              counts = new List<RequestCount>();
        /// <summary>number of sent or queued client events and its message and change subscriptions</summary>
        [Property (Name =                              "event")]
                public  EventDelivery?                  ev;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>number of sent or queued client events and its message and change subscriptions</summary>
    public struct EventDelivery {
        /// <summary>number of events sent to a client</summary>
                public  int                             seq;
        /// <summary>number of queued events not acknowledged by a client</summary>
                public  int                             queued;
        /// <summary>message / command subscriptions of a client</summary>
                public  List<string>                    messageSubs;
        /// <summary>change subscriptions of a client</summary>
                public  List<ChangeSubscription>        changeSubs;
    }
    
    /// <summary>change subscription for a specific container</summary>
    public sealed class ChangeSubscription
    {
        /// <summary>name of subscribed container</summary>
        [Req]   public  string                          container;
        /// <summary>type of subscribed changes like create, upsert, delete and patch</summary>
        [Req]   public  List<Change>                    changes;
        /// <summary>filter to narrow the amount of change events</summary>
                public  string                          filter;
    }
    
    /// <summary>aggregated counts of latest requests. Each record uses a specific aggregation interval.</summary>
    public sealed class HistoryHits {
        /// <summary>time in seconds for an aggregation interval</summary>
        [Req]   public  int                             id;
        /// <summary>number of requests executed in each interval</summary>
        [Req]   public  int[]                           counters;
        /// <summary>last update of the <see cref="HistoryHits"/> record</summary>
                public  int                             lastUpdate;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    
    // --- commands
    public sealed class ClearStats { }
    public sealed class ClearStatsResult { }
}
