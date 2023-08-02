// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Event;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Json.Fliox.Hub.Client
{
    public struct ClientOptions
    {
        // --- public
        public IEventReceiver  EventReceiver
        {
            private get => eventReceiver;
            set {
                if (!client._intern.clientId.IsNull()) {
                    throw new InvalidOperationException($"cannot change {nameof(EventReceiver)} after assigning {nameof(FlioxClient.ClientId)}");
                }
                if (client.GetSyncCount() > 0) {
                    throw new InvalidOperationException($"cannot change {nameof(EventReceiver)} after calling {nameof(FlioxClient.SyncTasks)}()");
                }
                if (!client._readonly.hub.SupportPushEvents) {
                    throw new InvalidOperationException("used hub does not SupportPushEvents");
                }
                eventReceiver = value;
            }
        }

        // --- private
        [Browse(Never)] internal IEventReceiver eventReceiver;
        [Browse(Never)] internal FlioxClient    client;
    }
}