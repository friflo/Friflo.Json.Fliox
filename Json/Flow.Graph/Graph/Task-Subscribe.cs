// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    public class SubscribeTask<T> : SyncTask where T : Entity
    {
        internal            TaskState               state;
        internal readonly   HashSet<TaskType>       types;
        internal readonly   FilterOperation         filter;
        private  readonly   string                  filterLinq; // use as string identifier of a filter
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeTask<{typeof(T).Name}> (filter: {filterLinq})";
        

        internal SubscribeTask(HashSet<TaskType> types, FilterOperation filter) {
            this.types      = types;
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
    }
    
    public class ChangeListener
    {
        public virtual void OnSubscribeChanges(ChangesEvent changes, EntityStore store) {
            foreach (var task in changes.tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        var set = store.GetEntitySet(create.container);
                        set.SyncPeerEntities(create.entities);
                        break;
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        set = store.GetEntitySet(update.container);
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
        }
    }
}