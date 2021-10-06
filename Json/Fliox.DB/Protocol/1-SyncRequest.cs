// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- request -----------------------------------
    public sealed class SyncRequest : ProtocolRequest
    {
        /// <summary>
        /// Specify an optional id to identify the client performing a request by a host.
        /// In case the request contains a <see cref="SubscribeChanges"/> <see cref="ProtocolRequest.clientId"/> is required to
        /// enable sending <see cref="SubscriptionEvent"/>'s to the desired subscriber.
        /// </summary>
        [Fri.Property(Name = "user")]   public  JsonKey                 userId;
                                        public  string                  token;
        /// <summary>
        /// <see cref="eventAck"/> is used to ensure (change) events are delivered reliable.
        /// A client set <see cref="eventAck"/> to the last received <see cref="ProtocolEvent.seq"/> in case
        /// it has subscribed to database changes by a <see cref="SubscribeChanges"/> task.
        /// Otherwise <see cref="eventAck"/> is null.
        /// </summary>
        [Fri.Property(Name = "ack")]    public  int?                    eventAck;
        [Fri.Required]                  public  List<SyncRequestTask>   tasks;
                                        public  JsonValue               info;
        
        internal override                       MessageType             MessageType => MessageType.sync;
    }
}
