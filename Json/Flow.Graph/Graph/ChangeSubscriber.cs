// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    public class ChangeSubscriber
    {
        private readonly    EntityStore                     store;
        private readonly    Dictionary<Type, EntityChanges> results     = new Dictionary<Type, EntityChanges>();
        
        /// Either <see cref="synchronizationContext"/> or <see cref="changeQueue"/> is set. Never both.
        private readonly    SynchronizationContext          synchronizationContext;
        /// Either <see cref="synchronizationContext"/> or <see cref="changeQueue"/> is set. Never both.
        private readonly    ConcurrentQueue <ChangeEvent>   changeQueue;
        
        public              int                             ChangeSequence     { get; private set ;}
        public              ChangeInfo<T>                   GetChangeInfo<T>() where T : Entity => GetChanges<T>().sum;
        
        public ChangeSubscriber (EntityStore store, SynchronizationContext synchronizationContext) {
            this.store                  = store;
            this.synchronizationContext = synchronizationContext;
        }
        
        public ChangeSubscriber (EntityStore store) {
            this.store                  = store;
            this.changeQueue            = new ConcurrentQueue <ChangeEvent> ();
        }
        
        public virtual void EnqueueChange(ChangeEvent change) {
            if (changeQueue != null) {
                changeQueue.Enqueue(change);
                return;
            }
            synchronizationContext.Post(delegate {
                OnChange(change);
            }, null);
        }
        
        public void ProcessChanges() {
            if (synchronizationContext != null) {
                throw new InvalidOperationException("ChangeSubscriber initialized with SynchronizationContext");
            }
            while (changeQueue.TryDequeue(out ChangeEvent changeEvent)) {
                OnChange(changeEvent);
            }
        }

        protected virtual void OnChange(ChangeEvent change) {
            ChangeSequence++;
            foreach (var task in change.tasks) {
                EntitySet set;
                switch (task.TaskType) {
                    
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        set = store.GetEntitySet(create.container);
                        // apply changes only if subscribed
                        if (set.GetSubscription() == null)
                            continue;
                        set.SyncPeerEntities(create.entities);
                        break;
                    
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        set = store.GetEntitySet(update.container);
                        // apply changes only if subscribed
                        if (set.GetSubscription() == null)
                            continue;
                        set.SyncPeerEntities(update.entities);
                        break;
                    
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        set = store.GetEntitySet(delete.container);
                        // apply changes only if subscribed
                        if (set.GetSubscription() == null)
                            continue;
                        set.DeletePeerEntities (delete.ids);
                        break;
                    
                    case TaskType.patch:
                        var patches = (PatchEntities)task;
                        set = store.GetEntitySet(patches.container);
                        // apply changes only if subscribed
                        if (set.GetSubscription() == null)
                            continue;
                        set.PatchPeerEntities(patches.patches);
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
        
        protected EntityChanges<T> GetEntityChanges<T>(ChangeEvent change) where T : Entity {
            var result  = GetChanges<T>();
            var set     = result.set;
            result.Clear();
            
            foreach (var task in change.tasks) {
                switch (task.TaskType) {
                    
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        if (create.container != set.name)
                            continue;
                        foreach (var entityPair in create.entities) {
                            string  id      = entityPair.Key;
                            var     peer    = set.GetPeerById(id);
                            var     entity  = peer.Entity;
                            result.creates.Add(entity.id, entity);
                        }
                        result.info.creates += create.entities.Count;
                        break;
                    
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        if (update.container != set.name)
                            continue;
                        foreach (var entityPair in update.entities) {
                            string  id      = entityPair.Key;
                            var     peer    = set.GetPeerById(id);
                            var     entity  = peer.Entity;
                            result.updates.Add(entity.id, entity);
                        }
                        result.info.updates += update.entities.Count;
                        break;
                    
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        if (delete.container != set.name)
                            continue;
                        foreach (var id in delete.ids) {
                            result.deletes.Add(id);
                        }
                        result.info.deletes += delete.ids.Count;
                        break;
                    
                    case TaskType.patch:
                        var patch = (PatchEntities)task;
                        if (patch.container != set.name)
                            continue;
                        foreach (var pair in patch.patches) {
                            string      id          = pair.Key;
                            var         peer        = set.GetPeerById(id);
                            var         entity      = peer.Entity;
                            EntityPatch entityPatch = pair.Value;
                            var         changePatch = new ChangePatch<T>(entity, entityPatch.patches);
                            result.patches.Add(id, changePatch);
                        }
                        result.info.patches += patch.patches.Count;
                        break;
                }
            }
            result.sum.Add(result.info);
            return result;
        }
    }
    
    public class ChangeInfo<T> : ChangeInfo where T : Entity
    {
        public bool IsEqual(ChangeInfo<T> other) {
            return creates == other.creates &&
                   updates == other.updates &&
                   deletes == other.deletes &&
                   patches == other.patches;
        }
    }
    
    public abstract class EntityChanges { }
    
    public class EntityChanges<T> : EntityChanges where T : Entity {
        public   readonly   Dictionary<string, T>               creates = new Dictionary<string, T>();
        public   readonly   Dictionary<string, T>               updates = new Dictionary<string, T>();
        public   readonly   HashSet   <string>                  deletes = new HashSet   <string>();
        public   readonly   Dictionary<string, ChangePatch<T>>  patches = new Dictionary<string, ChangePatch<T>>();

        public   readonly   ChangeInfo<T>                       sum     = new ChangeInfo<T>();
        public   readonly   ChangeInfo<T>                       info    = new ChangeInfo<T>();

        internal readonly   EntitySet<T>                        set;
        
        public override     string                              ToString() => info.ToString();       

        internal EntityChanges(EntitySet<T> set) {
            this.set = set;
        }

        internal void Clear() {
            creates.Clear();
            updates.Clear();
            deletes.Clear();
            patches.Clear();
            //
            info.Clear();
        }
    }
    
    public readonly struct ChangePatch<T> where T : Entity {
        public readonly T               entity;
        public readonly List<JsonPatch> patches;

        public override string          ToString() => entity.id;

        public ChangePatch(T entity, List<JsonPatch> patches) {
            this.entity     = entity;
            this.patches    = patches;
        }
    }
}