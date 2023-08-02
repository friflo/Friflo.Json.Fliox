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
        /// <summary>
        /// An <see cref="EventReceiver"/> send subscribed events to a <see cref="FlioxClient"/> instance.<br/>
        /// Its its currently only used for testing.<br/>
        /// It must be set before calling <see cref="FlioxClient.SyncTasks"/> or assigning <see cref="FlioxClient.ClientId"/>.
        /// </summary>
        public IEventReceiver  EventReceiver { private get => eventReceiver; set => SetEventReceiver(value); }

        // --- private
        [Browse(Never)] internal IEventReceiver eventReceiver;
        [Browse(Never)] internal FlioxClient    client;
        
        private void SetEventReceiver(IEventReceiver receiver)
        {
            if (!client._intern.clientId.IsNull()) {
                throw new InvalidOperationException($"cannot change {nameof(EventReceiver)} after assigning {nameof(FlioxClient.ClientId)}");
            }
            if (client.GetSyncCount() > 0) {
                throw new InvalidOperationException($"cannot change {nameof(EventReceiver)} after calling {nameof(FlioxClient.SyncTasks)}()");
            }
            if (!client._readonly.hub.SupportPushEvents) {
                throw new InvalidOperationException("used hub does not SupportPushEvents");
            }
            eventReceiver = receiver;
        }
    }
}