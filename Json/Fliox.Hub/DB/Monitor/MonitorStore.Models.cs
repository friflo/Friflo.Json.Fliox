// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    // ---------------------------------- entity models ----------------------------------
    /// <summary>number of requests and tasks executed by the host. Container contains always a single record</summary>
    public sealed class HostHits {
        /// <summary>host name</summary>
        [Required]  public  ShortString         id;
        /// <summary>number of executed requests and tasks per database</summary>
                    public  RequestCount        counts;
                        
        public override     string              ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>all user clients and number of executed user requests and tasks</summary>
    public sealed class UserHits {
        /// <summary>user id </summary>
        [Required]  public  ShortString         id;
        /// <summary>list of clients owned by a user</summary>
        [Relation(nameof(MonitorStore.clients))]
        [Required]  public  List<ShortString>   clients;
        /// <summary>number executed requests and tasks per database</summary>
                    public  List<RequestCount>  counts = new List<RequestCount>();
                        
        public override     string              ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>information about requests, tasks, events and subscriptions of a client</summary>
    public sealed class ClientHits {
        /// <summary>client id </summary>
        [Required]  public  ShortString         id;
        /// <summary>user owning the client</summary>
        [Relation(nameof(MonitorStore.users))]
        [Required]  public  ShortString         user;
        /// <summary>number executed requests and tasks per database</summary>
                    public  List<RequestCount>  counts = new List<RequestCount>();
        /// <summary>number of sent or queued client events and its message and change subscriptions</summary>
                    public  SubscriptionEvents? subscriptionEvents;
                        
        public override     string              ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    

    
    /// <summary>aggregated counts of latest requests. Each record uses a specific aggregation interval.</summary>
    public sealed class HistoryHits {
        /// <summary>time in seconds for an aggregation interval</summary>
        [Required]  public  int                 id;
        /// <summary>number of requests executed in each interval</summary>
        [Required]  public  int[]               counters;
        /// <summary>last update of the <see cref="HistoryHits"/> record</summary>
                    public  int                 lastUpdate;
                        
        public override     string              ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    
    // ---------------------------- command models - aka DTO's ---------------------------
    public sealed class ClearStats { }
    public sealed class ClearStatsResult { }
}