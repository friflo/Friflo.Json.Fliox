// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
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

        public Task<bool> ProcessEvent(ProtocolEvent protocolEvent) {
            if (!protocolEvent.dstClientId.IsEqual(client._intern.clientId))
                throw new InvalidOperationException("Expect ProtocolEvent.dstId == FlioxClient.clientId");
            

            var eventMessage = protocolEvent as EventMessage;
            if (eventMessage == null)
                return Task.FromResult(true);

            // Console.WriteLine($"----- ProcessEvent. events: {eventMessages.events.Length}");
            foreach (var ev in eventMessage.events) {
                // Skip already received events
                if (client._intern.lastEventSeq >= ev.seq)
                    continue; // could also break as all subsequent events 
            
                client._intern.lastEventSeq = ev.seq;                
                client._intern.eventProcessor.EnqueueEvent(client, ev);
            }

            return Task.FromResult(true);
        }
    }
}