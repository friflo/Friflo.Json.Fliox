// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    public abstract class EventReceiver {
        public abstract bool    IsOpen ();
        public abstract bool    IsRemoteTarget ();
        /// <summary>Send a serialized <see cref="EventMessage"/> to the receiver</summary>
        public abstract void    SendEvent(in RemoteEvent eventMessage);
    }
}