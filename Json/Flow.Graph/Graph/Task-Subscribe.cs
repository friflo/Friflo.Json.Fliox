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
        private ChangesEvent    changes;
        private EntityStore     store;
            
        public virtual void OnSubscribeChanges(ChangesEvent changes, EntityStore store) {
            this.changes    = changes;
            this.store      = store;
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
                        var delete = (DeleteEntities)task;
                        // todo implement
                        break;
                    case TaskType.patch:
                        // todo implement
                        break;
                }
            }
        }
        
        protected EntitySetChanges<T> GetEntitySetChanges<T>() where T : Entity {
            var             creates = new List<T>();
            var             updates = new List<T>();
            HashSet<string> deletes = null;
            var set = (EntitySet<T>) store._intern.setByType[typeof(T)];
            
            foreach (var task in changes.tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        if (create.container != set.name)
                            continue;
                        creates.Capacity = create.entities.Count;
                        foreach (var entityPair in create.entities) {
                            string key = entityPair.Key;
                            var peer = set.GetPeerById(key);
                            creates.Add(peer.Entity);
                        }
                        break;
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        if (update.container != set.name)
                            continue;
                        updates.Capacity = update.entities.Count;
                        foreach (var entityPair in update.entities) {
                            string key = entityPair.Key;
                            var peer = set.GetPeerById(key);
                            updates.Add(peer.Entity);
                        }
                        break;
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        deletes = delete.ids;
                        break;
                }
            }
            var setChanges = new EntitySetChanges<T>(creates, updates, deletes);
            return setChanges;
        }
    }
    
    public class EntitySetChanges<T> where T : Entity {
        public readonly List<T>         creates;
        public readonly List<T>         updates;
        public readonly HashSet<string> deletes;
        
        internal EntitySetChanges(List<T> creates, List<T> updates, HashSet<string> deletes) {
            this.creates = creates;
            this.updates = updates;
            this.deletes = deletes ?? new HashSet<string>();
        }
    }
}