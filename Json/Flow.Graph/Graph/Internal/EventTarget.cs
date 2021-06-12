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
        readonly EntityStore store;
        
        internal EventTarget (EntityStore store) {
            this.store = store; 
        } 
            
        // --- IMessageTarget 
        public Task<bool> SendEvent(DatabaseEvent ev, SyncContext syncContext) {
            var changesEvent = ev as ChangesEvent;
            if (changesEvent == null)
                return Task.FromResult(true);
            foreach (var task in changesEvent.tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        var set = store._intern.setByName[create.container];
                        set.SyncPeerEntities(create.entities);
                        break;
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        set = store._intern.setByName[update.container];
                        set.SyncPeerEntities(update.entities);
                        break;
                    case TaskType.delete:
                        // todo implement
                        break;
                    case TaskType.patch:
                        // todo implement
                        break;
                }
            }
            store._intern.changeListener?.OnSubscribeChanges(changesEvent);

            return Task.FromResult(true);
        }
        
        public bool IsOpen () {
            return true;
        }
    }
}