// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class EventReceiver : IEventReceiver
    {
        private readonly FlioxClient    client;
        
        internal EventReceiver (FlioxClient client) {
            this.client = client; 
        } 
            
        // --- IEventReceiver
        public bool     IsRemoteTarget ()   => false;
        public bool     IsOpen ()           => true;

        public bool SendEvent(ProtocolEvent protocolEvent, in ProcessEventRemoteArgs args) {
            if (!protocolEvent.dstClientId.IsEqual(client._intern.clientId))
                throw new InvalidOperationException("Expect ProtocolEvent.dstId == FlioxClient.clientId");
            

            var eventMessage = protocolEvent as EventMessage;
            if (eventMessage == null)
                return true;
            client._intern.eventProcessor.EnqueueEvent(client, eventMessage);

            return true;
        }
    }
}