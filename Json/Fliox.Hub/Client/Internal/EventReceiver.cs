// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class ClientEventReceiver : EventReceiver
    {
        private readonly FlioxClient    client;
        
        internal ClientEventReceiver (FlioxClient client) {
            this.client = client; 
        } 
            
        // --- IEventReceiver
        public override bool    IsRemoteTarget ()   => false;
        public override bool    IsOpen ()           => true;

        public override void    SendEvent(EventMessage eventMessage, bool reusedEvent, in SendEventArgs args) {
            if (!eventMessage.dstClientId.IsEqual(client._intern.clientId))
                throw new InvalidOperationException("Expect ProtocolEvent.dstId == FlioxClient.clientId");
            
            client._intern.eventProcessor.EnqueueEvent(client, eventMessage, reusedEvent);
        }
    }
}