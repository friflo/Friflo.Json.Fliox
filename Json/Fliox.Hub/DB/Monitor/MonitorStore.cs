// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

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
        // --- containers
        public  readonly    EntitySet <ShortString, HostHits>       hosts;
        public  readonly    EntitySet <ShortString, UserHits>       users;
        public  readonly    EntitySet <ShortString, ClientHits>     clients;
        public  readonly    EntitySet <int,         HistoryHits>    histories;

        public MonitorStore(FlioxHub hub, string dbName = null) : base(hub, dbName) { }
        
        /// <summary>Reset all request, task and event counters</summary>
        public CommandTask<ClearStatsResult> ClearStats(ClearStats value = null) => send.Command<ClearStats, ClearStatsResult>(value);
    }
}
