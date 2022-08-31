// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    // ---------------------------------- entity models ----------------------------------
    /// <summary>number of requests and tasks executed by the host. Container contains always a single record</summary>
    public sealed class HostHits {
        /// <summary>host name</summary>
        [Required]  public  JsonKey             id;
        /// <summary>number of executed requests and tasks per database</summary>
                    public  RequestCount        counts;
                        
        public override     string              ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>all user clients and number of executed user requests and tasks</summary>
    public sealed class UserHits {
        /// <summary>user id </summary>
        [Required]  public  JsonKey             id;
        /// <summary>list of clients owned by a user</summary>
        [Relation(nameof(MonitorStore.clients))]
        [Required]  public  List<JsonKey>       clients;
        /// <summary>number executed requests and tasks per database</summary>
                    public  List<RequestCount>  counts = new List<RequestCount>();
                        
        public override     string              ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>information about requests, tasks, events and subscriptions of a client</summary>
    public sealed class ClientHits {
        /// <summary>client id </summary>
        [Required]  public  JsonKey             id;
        /// <summary>user owning the client</summary>
        [Relation(nameof(MonitorStore.users))]
        [Required]  public  JsonKey             user;
        /// <summary>number executed requests and tasks per database</summary>
                    public  List<RequestCount>  counts = new List<RequestCount>();
        /// <summary>number of sent or queued client events and its message and change subscriptions</summary>
        [Serialize (Name =                     "event")]
                    public  EventDelivery?      ev;
                        
        public override     string              ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>number of sent or queued client events and its message and change subscriptions</summary>
    public struct EventDelivery {
        /// <summary>number of events sent to a client</summary>
        public  int                             seq;
        /// <summary>number of queued events not acknowledged by a client</summary>
        public  int                             queued;
        /// <summary>true if client is instructed to queue events for reliable event delivery in case of reconnects</summary>
        public  bool                            queueEvents;
        /// <summary>true if client is connected. Non remote client are always connected</summary>
        public  bool                            connected;
        /// <summary>message / command subscriptions of a client</summary>
        public  List<string>                    messageSubs;
        /// <summary>change subscriptions of a client</summary>
        public  List<ChangeSubscription>        changeSubs;
    }
    
    /// <summary>change subscription for a specific container</summary>
    public sealed class ChangeSubscription
    {
        /// <summary>name of subscribed container</summary>
        [Required]  public  string              container;
        /// <summary>type of subscribed changes like create, upsert, delete and patch</summary>
        [Required]  public  List<EntityChange>  changes;
        /// <summary>filter to narrow the amount of change events</summary>
                    public  string              filter;
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