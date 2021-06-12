// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    public class EventTarget : IEventTarget
    {
        private readonly EntityStore store;
        
        internal EventTarget (EntityStore store) {
            this.store = store; 
        } 
            
        // --- IEventTarget 
        public Task<bool> SendEvent(DatabaseEvent ev, SyncContext syncContext) {
            var changesEvent = ev as ChangesEvent;
            if (changesEvent == null)
                return Task.FromResult(true);

            store._intern.changeListener?.OnSubscribeChanges(changesEvent, store);

            return Task.FromResult(true);
        }
        
        public bool IsOpen () {
            return true;
        }
    }
}