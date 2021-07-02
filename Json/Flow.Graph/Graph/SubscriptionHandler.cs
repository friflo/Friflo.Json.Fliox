// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    public class SubscriptionHandler
    {
        private readonly    EntityStore                         store;
        private readonly    Dictionary<Type, EntityChanges>     results   = new Dictionary<Type, EntityChanges>();
        private readonly    List<Message>                       messages  = new List<Message>();
        
        /// Either <see cref="synchronizationContext"/> or <see cref="eventQueue"/> is set. Never both.
        private readonly    SynchronizationContext              synchronizationContext;
        /// Either <see cref="synchronizationContext"/> or <see cref="eventQueue"/> is set. Never both.
        private readonly    ConcurrentQueue <SubscriptionEvent> eventQueue;
        
        public              int                                 EventSequence     { get; private set ;}
        public              ChangeInfo<T>                       GetChangeInfo<T>() where T : Entity => GetChanges<T>().sum;
        
        /// <summary>
        /// Creates a <see cref="SubscriptionHandler"/> with the specified <see cref="synchronizationContext"/>
        /// The <see cref="synchronizationContext"/> is required to ensure that <see cref="ProcessEvent"/> is called on the
        /// same thread as all other API calls of <see cref="EntityStore"/> and <see cref="EntitySet{T}"/>.
        /// <para>
        ///   In case of a UI application like WinForms or WPF <see cref="SynchronizationContext.Current"/> can be used
        /// </para> 
        /// <para>
        ///   In case of a Console application <see cref="SingleThreadSynchronizationContext"/> can be used.
        /// </para> 
        /// </summary>
        public SubscriptionHandler (EntityStore store, SynchronizationContext synchronizationContext) {
            this.store                  = store;
            this.synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        
        /// <summary>
        /// Creates a <see cref="SubscriptionHandler"/> without a <see cref="synchronizationContext"/>
        /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
        /// <see cref="EntityStore"/>.
        /// This allows to specify the exact code point in an application (e.g. Unity) where <see cref="SubscriptionEvent"/>'s
        /// are applied to the <see cref="EntityStore"/>.
        /// </summary>
        public SubscriptionHandler (EntityStore store) {
            this.store                  = store;
            this.eventQueue             = new ConcurrentQueue <SubscriptionEvent> ();
        }
        
        public virtual void EnqueueEvent(SubscriptionEvent ev) {
            if (eventQueue != null) {
                eventQueue.Enqueue(ev);
                return;
            }
            synchronizationContext.Post(delegate {
                ProcessEvent(ev);
            }, null);
        }
        
        /// <summary>
        /// Need to be called frequently if <see cref="SubscriptionHandler"/> is initialized without a <see cref="SynchronizationContext"/>.
        /// </summary>
        public void ProcessEvents() {
            if (synchronizationContext != null) {
                throw new InvalidOperationException("SubscriptionHandler initialized with SynchronizationContext");
            }
            while (eventQueue.TryDequeue(out SubscriptionEvent subscribeEvent)) {
                ProcessEvent(subscribeEvent);
            }
        }

        /// <summary>
        /// Process the <see cref="SubscriptionEvent.tasks"/> of the given <see cref="SubscriptionEvent"/>.
        /// These <see cref="SubscriptionEvent.tasks"/> are "messages" resulting from subscriptions registered by
        /// methods like <see cref="EntitySet{T}.SubscribeChanges"/> or <see cref="EntityStore.SubscribeAllChanges"/>.
        /// <br></br>
        /// Tasks notifying about database changes are applied to the <see cref="EntityStore"/> the <see cref="SubscriptionHandler"/>
        /// is attached to.
        /// Types of database changes refer to <see cref="Change.create"/>ed, <see cref="Change.update"/>ed,
        /// <see cref="Change.delete"/>ed and <see cref="Change.patch"/>ed entities.
        /// <br></br>
        /// Tasks notifying "messages" are ignored. These message subscriptions are registered by <see cref="EntityStore.SubscribeMessage"/>.
        /// </summary>
        protected virtual void ProcessEvent(SubscriptionEvent ev) {
            EventSequence++;
            if (store._intern.disposed)  // store may already be disposed
                return;
            
            foreach (var task in ev.tasks) {
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
                    
                    case TaskType.message:
                        var message = (SendMessage)task;
                        // callbacks require their own reader as store._intern.jsonMapper.reader cannot be used.
                        // This jsonMapper is used in various threads caused by .ConfigureAwait(false) continuations
                        // and ProcessEvent() can be called concurrently from the "main" thread.
                        var reader = store._intern.messageReader;
                        if (store._intern.subscriptions.TryGetValue(message.name, out MessageSubscriber subscriber)) {
                            subscriber.InvokeCallbacks(reader, message.value);    
                        }
                        foreach (var sub in store._intern.subscriptionsPrefix) {
                            if (message.name.StartsWith(sub.name)) {
                                sub.InvokeCallbacks(reader, message.value);
                            }
                        }
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
        
        protected List<Message> GetMessages(SubscriptionEvent subscriptionEvent) {
            messages.Clear();
            foreach (var task in subscriptionEvent.tasks) {
                switch (task.TaskType) {
                    
                    case TaskType.message:
                        var sendMessage = (SendMessage)task;
                        var reader = store._intern.messageReader;
                        var message = new Message(sendMessage.name, sendMessage.value.json, reader);
                        messages.Add(message);
                        break;
                }
            }
            return messages;
        }
        
        protected EntityChanges<T> GetEntityChanges<T>(SubscriptionEvent subscriptionEvent) where T : Entity {
            var result  = GetChanges<T>();
            var set     = result.set;
            result.Clear();
            
            foreach (var task in subscriptionEvent.tasks) {
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