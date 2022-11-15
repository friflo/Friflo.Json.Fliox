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
        public abstract void    SendEvent(EventMessage eventMessage, bool reusedEvent, in SendEventArgs args);
    }
    
    /// <summary>
    /// Optimization for sending remote events.<br/>
    /// Avoids frequent allocation of <see cref="eventBuffer"/> lists
    /// </summary>
    public readonly struct SendEventArgs
    {
        internal readonly   ObjectMapper            mapper;
        internal readonly   List<RemoteSyncEvent>   eventBuffer;
            
        internal SendEventArgs(ObjectMapper mapper, List<RemoteSyncEvent> eventBuffer) {
            this.mapper         = mapper;
            this.eventBuffer    = eventBuffer;
        }
    }
}