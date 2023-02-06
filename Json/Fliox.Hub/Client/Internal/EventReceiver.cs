// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Event;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class ClientEventReceiver : EventReceiver
    {
        private readonly FlioxClient    client;
        
        internal ClientEventReceiver (FlioxClient client) {
            this.client = client; 
        } 
            
        // --- IEventReceiver
        protected internal override bool    IsRemoteTarget ()   => false;
        protected internal override bool    IsOpen ()           => true;

        protected internal override void    SendEvent(in ClientEvent clientEvent) {
            if (!clientEvent.dstClientId.IsNull() && !clientEvent.dstClientId.IsEqual(client._intern.clientId)) {
                throw new InvalidOperationException("Expect event target client id == FlioxClient.clientId");
            }
            client._intern.eventProcessor.EnqueueEvent(client, clientEvent.message);
        }
    }
}