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
        
        public              ChangeCounts<T>                 GetChangeCounts<T>() where T : Entity => GetChanges<T>().sum;
            
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
        
        private EntityChanges<T> GetChanges<T> () where T : Entity {
            if (!results.TryGetValue(typeof(T), out var result)) {
                var resultTyped = new EntityChanges<T>();
                results.Add(typeof(T), resultTyped);
                return resultTyped;
            }
            return (EntityChanges<T>)result;
        }
        
        protected EntityChanges<T> GetEntityChanges<T>() where T : Entity {
            var typedResult = GetChanges<T>();
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
                            typedResult.sum.creates++;
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
                            typedResult.sum.updates++;
                        }
                        break;
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        if (delete.container != set.name)
                            continue;
                        typedResult.deletes = delete.ids;
                        typedResult.sum.deletes += delete.ids.Count;
                        break;
                }
            }
            typedResult.sum.changes += typedResult.Count;
            return typedResult;
        }
    }
    
    public class ChangeCounts<T> where T : Entity {
        public  int changes;
        public  int creates;
        public  int updates;
        public  int deletes;

        public override string ToString() => $"({changes}, {creates}, {updates}, {deletes})";

        public void AddChanges(EntityChanges<T> entityChanges) {
            changes += entityChanges.Count;
            creates += entityChanges.creates.Count;
            updates += entityChanges.updates.Count;
            deletes += entityChanges.deletes.Count;
        }

        public bool IsEqual(ChangeCounts<T> other) {
            return changes == other.changes &&
                   creates == other.creates &&
                   updates == other.updates &&
                   deletes == other.deletes;
        }
    }
    
    public abstract class EntityChanges { }
    
    public class EntityChanges<T> : EntityChanges where T : Entity {
        public  readonly    List<T>         creates = new List<T>();
        public  readonly    List<T>         updates = new List<T>();
        public              HashSet<string> deletes;
        public  readonly    ChangeCounts<T> sum = new ChangeCounts<T>();
        
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