// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public class ChangeSubscriber
    {
        public              int                             onChangeCount;
        public              ChangeInfo<T>                   GetChangeInfo<T>() where T : Entity => GetChanges<T>().sum;
        
        private             ChangesEvent                    changes;
        private             EntityStore                     store;
        private readonly    Dictionary<Type, EntityChanges> results = new Dictionary<Type, EntityChanges>();
            
        public virtual void OnChanges(ChangesEvent changes, EntityStore store) {
            onChangeCount++;
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
                        var patches = (PatchEntities)task;
                        // todo implement
                        break;
                }
            }
        }
        
        private EntityChanges<T> GetChanges<T> () where T : Entity {
            if (!results.TryGetValue(typeof(T), out var result)) {
                var set         = (EntitySet<T>) store._intern.setByType[typeof(T)];
                var resultTyped = new EntityChanges<T>(set);
                results.Add(typeof(T), resultTyped);
                return resultTyped;
            }
            return (EntityChanges<T>)result;
        }
        
        protected EntityChanges<T> GetEntityChanges<T>() where T : Entity {
            var result  = GetChanges<T>();
            var set     = result.set;
            result.Clear();
            
            foreach (var task in changes.tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        if (create.container != set.name)
                            continue;
                        foreach (var entityPair in create.entities) {
                            string key = entityPair.Key;
                            var peer = set.GetPeerById(key);
                            result.creates.Add(peer.Entity);
                            result.sum.creates++;
                        }
                        break;
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        if (update.container != set.name)
                            continue;
                        foreach (var entityPair in update.entities) {
                            string key = entityPair.Key;
                            var peer = set.GetPeerById(key);
                            result.updates.Add(peer.Entity);
                            result.sum.updates++;
                        }
                        break;
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        if (delete.container != set.name)
                            continue;
                        result.deletes = delete.ids;
                        result.sum.deletes += delete.ids.Count;
                        break;
                    case TaskType.patch:
                        var patch = (PatchEntities)task;
                        if (patch.container != set.name)
                            continue;
                        // todo
                        result.sum.patches++;
                        break;
                }
            }
            result.sum.changes += result.Count;
            return result;
        }
    }
    
    public class ChangeInfo<T> where T : Entity {
        public  int changes;
        public  int creates;
        public  int updates;
        public  int deletes;
        public  int patches;

        public override string ToString() => $"({changes}, {creates}, {updates}, {deletes}, {patches})";

        public void AddChanges(EntityChanges<T> entityChanges) {
            changes += entityChanges.Count;
            creates += entityChanges.creates.Count;
            updates += entityChanges.updates.Count;
            deletes += entityChanges.deletes.Count;
            // deletes += entityChanges.patches.Count; // todo
        }

        public bool IsEqual(ChangeInfo<T> other) {
            return changes == other.changes &&
                   creates == other.creates &&
                   updates == other.updates &&
                   deletes == other.deletes &&
                   patches == other.patches;
        }
    }
    
    public abstract class EntityChanges { }
    
    public class EntityChanges<T> : EntityChanges where T : Entity {
        public   readonly   List<T>         creates = new List<T>();
        public   readonly   List<T>         updates = new List<T>();
        public              HashSet<string> deletes;
        public   readonly   ChangeInfo<T>   sum = new ChangeInfo<T>();
        
        internal readonly   EntitySet<T>    set;
        private  readonly   HashSet<string> deletesEmpty = new HashSet<string>(); 
        
        public          int                 Count => creates.Count + updates.Count + deletes.Count;
        
        internal EntityChanges(EntitySet<T> set) {
            this.set = set;
        }

        internal void Clear() {
            creates     .Clear();
            updates     .Clear();
            deletesEmpty.Clear();
            deletes = deletesEmpty;
        }
    }
}