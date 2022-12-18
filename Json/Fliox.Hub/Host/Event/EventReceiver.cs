// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    public abstract class EventReceiver {
        public abstract bool    IsOpen ();
        public abstract bool    IsRemoteTarget ();
        /// <summary>Send a serialized <see cref="EventMessage"/> to the receiver</summary>
        public abstract void    SendEvent(in RemoteEvent eventMessage);
    }
    
    /// <summary>
    /// Optimization for sending remote events.<br/>
    /// Avoids frequent allocations of <br/>
    /// <see cref="eventBuffer"/> lists <br/>
    /// <see cref="EventMessage"/> <br/>
    /// <see cref="EventMessage.events"/> <br/>
    /// </summary>
    public readonly struct SendEventArgs
    {
        internal readonly   ObjectMapper            mapper;
        internal readonly   List<RemoteSyncEvent>   eventBuffer;
        internal readonly   EventMessage            eventMessage;

        internal SendEventArgs(ObjectMapper mapper, EventMessage eventMessage, List<RemoteSyncEvent> eventBuffer) {
            this.mapper         = mapper;
            this.eventMessage   = eventMessage;
            this.eventBuffer    = eventBuffer;
        }
    }
}