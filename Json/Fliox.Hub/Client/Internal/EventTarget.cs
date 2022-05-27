// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class EventTarget : IEventTarget
    {
        private readonly FlioxClient    client;
        
        internal EventTarget (FlioxClient client) {
            this.client = client; 
        } 
            
        // --- IEventTarget
        public bool     IsOpen ()   => true;

        public Task<bool> ProcessEvent(ProtocolEvent ev) {
            if (!ev.dstClientId.IsEqual(client._intern.clientId))
                throw new InvalidOperationException("Expect ProtocolEvent.dstId == FlioxClient.clientId");
            
            // Skip already received events
            if (client._intern.lastEventSeq >= ev.seq)
                return Task.FromResult(true);
            
            client._intern.lastEventSeq = ev.seq;
            var eventMessage = ev as EventMessage;
            if (eventMessage == null)
                return Task.FromResult(true);

            client._intern.eventProcessor.EnqueueEvent(client, eventMessage);

            return Task.FromResult(true);
        }
    }
}