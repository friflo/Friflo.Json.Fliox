// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    public class EventTarget : IEventTarget
    {
        private readonly EntityStore    store;
        
        internal EventTarget (EntityStore store) {
            this.store = store; 
        } 
            
        // --- IEventTarget
        public bool     IsOpen ()   => true;

        public Task<bool> ProcessEvent(DatabaseEvent ev, SyncContext syncContext) {
            if (ev.targetId != store._intern.clientId)
                throw new InvalidOperationException("Expect DatabaseEvent.clientId == EntityStore.clientId");
            
            // Skip already received events
            if (store._intern.lastEvent >= ev.eventId)
                return Task.FromResult(true);
            
            store._intern.lastEvent = ev.eventId;
            var changesEvent = ev as ChangesEvent;
            if (changesEvent == null)
                return Task.FromResult(true);

            store._intern.changeSubscriber?.OnChanges(changesEvent, store);

            return Task.FromResult(true);
        }
    }
}