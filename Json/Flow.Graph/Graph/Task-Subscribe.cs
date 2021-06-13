// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
        private             ChangesEvent                    changes;
        private             EntityStore                     store;
        private readonly    Dictionary<Type, EntityChanges> results = new Dictionary<Type, EntityChanges>();
            
        public virtual void OnChanges(ChangesEvent changes, EntityStore store) {
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
        
        protected EntityChanges<T> GetEntityChanges<T>() where T : Entity {
            if (!results.TryGetValue(typeof(T), out var result)) {
                result = new EntityChanges<T>();
                results.Add(typeof(T), result);
            }
            var typedResult = (EntityChanges<T>)result;
            var set         = (EntitySet<T>) store._intern.setByType[typeof(T)];
            typedResult.Clear();
            
            foreach (var task in changes.tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        if (create.container != set.name)
                            continue;
                        foreach (var entityPair in create.entities) {
                            string key = entityPair.Key;
                            var peer = set.GetPeerById(key);
                            typedResult.creates.Add(peer.Entity);
                        }
                        break;
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        if (update.container != set.name)
                            continue;
                        foreach (var entityPair in update.entities) {
                            string key = entityPair.Key;
                            var peer = set.GetPeerById(key);
                            typedResult.updates.Add(peer.Entity);
                        }
                        break;
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        if (delete.container != set.name)
                            continue;
                        typedResult.deletes = delete.ids;
                        break;
                }
            }
            return typedResult;
        }
    }
    
    public abstract class EntityChanges { }
    
    public class EntityChanges<T> : EntityChanges where T : Entity {
        public  readonly    List<T>         creates = new List<T>();
        public  readonly    List<T>         updates = new List<T>();
        public              HashSet<string> deletes;
        
        private readonly    HashSet<string> deletesEmpty = new HashSet<string>(); 
        
        public          int                 Count => creates.Count + updates.Count + deletes.Count;
        
        internal EntityChanges() { }

        internal void Clear() {
            creates.Clear();
            updates.Clear();
            deletes = deletesEmpty;
        }
    }
}