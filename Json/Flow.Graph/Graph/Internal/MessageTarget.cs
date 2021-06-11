// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;


namespace Friflo.Json.Flow.Graph.Internal
{
    public class MessageTarget : IMessageTarget
    {
        readonly EntityStore store;
        
        internal MessageTarget (EntityStore store) {
            this.store = store; 
        } 
            
        // --- IMessageTarget 
        public Task<bool> SendMessage(PushMessage push, SyncContext syncContext) {
            var databaseMessage = push as DatabaseMessage;
            if (databaseMessage == null)
                return Task.FromResult(true);
            foreach (var task in databaseMessage.tasks) {
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
            store._intern.changeListener?.OnSubscribeChanges(databaseMessage);

            return Task.FromResult(true);
        }
        
        public bool IsOpen () {
            return true;
        }
    }
}