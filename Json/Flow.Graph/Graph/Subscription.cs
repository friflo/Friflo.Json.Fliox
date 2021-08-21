// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Graph.Internal.Id;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    public enum SubscriptionHandling {
        /// <summary>
        /// Used in <see cref="SubscriptionProcessor(EntityStore, SubscriptionHandling)"/> to enforce manual handling of subscription events. 
        /// </summary>
        Manual
    }
    
    public delegate void SubscriptionHandler (SubscriptionProcessor processor, SubscriptionEvent ev);
    
    public class SubscriptionProcessor
    {
        private readonly    EntityStore                         store;
        private readonly    Dictionary<Type, EntityChanges>     results   = new Dictionary<Type, EntityChanges>();
        private readonly    List<Message>                       messages  = new List<Message>();
        
        /// Either <see cref="synchronizationContext"/> or <see cref="eventQueue"/> is set. Never both.
        private readonly    SynchronizationContext              synchronizationContext;
        /// Either <see cref="synchronizationContext"/> or <see cref="eventQueue"/> is set. Never both.
        private readonly    ConcurrentQueue <SubscriptionEvent> eventQueue;
        
        public              int                                 EventSequence { get; private set ;}

        public override     string                              ToString() => $"EventSequence: {EventSequence}";

        /// <summary>
        /// Creates a <see cref="SubscriptionProcessor"/> with the specified <see cref="synchronizationContext"/>
        /// The <see cref="synchronizationContext"/> is required to ensure that <see cref="ProcessEvent"/> is called on the
        /// same thread as all other API calls of <see cref="EntityStore"/> and <see cref="EntitySet{TKey,T}"/>.
        /// <para>
        ///   In case of UI applications like WinForms, WPF or Unity <see cref="SynchronizationContext.Current"/> can be used.
        ///   If <see cref="synchronizationContext"/> is null it defaults to <see cref="SynchronizationContext.Current"/>.
        /// </para> 
        /// <para>
        ///   In case of a Console application where <see cref="SynchronizationContext.Current"/> is null
        ///   <see cref="SingleThreadSynchronizationContext"/> can be used.
        /// </para> 
        /// </summary>
        public SubscriptionProcessor (EntityStore store, SynchronizationContext synchronizationContext = null) {
            synchronizationContext      = synchronizationContext ?? SynchronizationContext.Current; 
            this.store                  = store;
            this.synchronizationContext = synchronizationContext;
        }
        
        /// <summary>
        /// Creates a <see cref="SubscriptionProcessor"/> without a <see cref="synchronizationContext"/>
        /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
        /// <see cref="EntityStore"/>.
        /// This allows to specify the exact code point in an application (e.g. Unity) where <see cref="SubscriptionEvent"/>'s
        /// are applied to the <see cref="EntityStore"/>.
        /// </summary>
        public SubscriptionProcessor (EntityStore store, SubscriptionHandling _) {
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
        /// Need to be called frequently if <see cref="SubscriptionProcessor"/> is initialized without a <see cref="SynchronizationContext"/>.
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
        /// methods like <see cref="EntitySet{TKey,T}.SubscribeChanges"/>, <see cref="EntityStore.SubscribeAllChanges"/> or
        /// <see cref="EntityStore.SubscribeMessage"/>.
        /// <br></br>
        /// Tasks notifying about database changes are applied to the <see cref="EntityStore"/> the <see cref="SubscriptionProcessor"/>
        /// is attached to.
        /// Types of database changes refer to <see cref="Change.create"/>ed, <see cref="Change.update"/>ed,
        /// <see cref="Change.delete"/>ed and <see cref="Change.patch"/>ed entities.
        /// <br></br>
        /// Tasks notifying "messages" are ignored. These message subscriptions are registered by <see cref="EntityStore.SubscribeMessage"/>.
        /// </summary>
        protected virtual void ProcessEvent(SubscriptionEvent ev) {
            if (store._intern.disposed)  // store may already be disposed
                return;
            EventSequence++;
            
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
                        var name = message.name;
                        // callbacks require their own reader as store._intern.jsonMapper.reader cannot be used.
                        // This jsonMapper is used in various threads caused by .ConfigureAwait(false) continuations
                        // and ProcessEvent() can be called concurrently from the "main" thread.
                        var reader = store._intern.messageReader;
                        if (store._intern.subscriptions.TryGetValue(name, out MessageSubscriber subscriber)) {
                            subscriber.InvokeCallbacks(reader, name, message.value);    
                        }
                        foreach (var sub in store._intern.subscriptionsPrefix) {
                            if (name.StartsWith(sub.name)) {
                                sub.InvokeCallbacks(reader, name, message.value);
                            }
                        }
                        break;
                }
            }
            var subHandler = store._intern.subscriptionHandler;
            // ReSharper disable once UseNullPropagation
            if (subHandler == null)
                return;
            subHandler(this, ev); // subHandler.Invoke(this, ev);
        }
        
        private EntityChanges<T> GetChanges<T> () where T : class {
            if (!results.TryGetValue(typeof(T), out var result)) {
                var set         = store.GetPeerSet<T>();
                var resultTyped = new EntityChanges<T>(set);
                results.Add(typeof(T), resultTyped);
                return resultTyped;
            }
            return (EntityChanges<T>)result;
        }
        
        public List<Message> GetMessages(SubscriptionEvent subscriptionEvent) {
            messages.Clear();
            foreach (var task in subscriptionEvent.tasks) {
                if (task.TaskType != TaskType.message)
                    continue;
                var sendMessage = (SendMessage)task;
                var reader  = store._intern.messageReader;
                var message = new Message(sendMessage.name, sendMessage.value.json, reader);
                messages.Add(message);
            }
            return messages;
        }
        
        public EntityChanges<T> GetEntityChanges<T>(SubscriptionEvent subscriptionEvent) where T : class {
            var result  = GetChanges<T>();
            var set     = result._set;
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
                            result.creates.Add(id, entity);
                        }
                        result.Info.creates += create.entities.Count;
                        break;
                    
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        if (update.container != set.name)
                            continue;
                        foreach (var entityPair in update.entities) {
                            string  id      = entityPair.Key;
                            var     peer    = set.GetPeerById(id);
                            var     entity  = peer.Entity;
                            result.updates.Add(id, entity);
                        }
                        result.Info.updates += update.entities.Count;
                        break;
                    
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        if (delete.container != set.name)
                            continue;
                        foreach (var id in delete.ids) {
                            result.deletes.Add(id);
                        }
                        result.Info.deletes += delete.ids.Count;
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
                        result.Info.patches += patch.patches.Count;
                        break;
                }
            }
            return result;
        }
    }
    
    public class ChangeInfo<T> : ChangeInfo where T : class
    {
        public bool IsEqual(ChangeInfo<T> other) {
            return creates == other.creates &&
                   updates == other.updates &&
                   deletes == other.deletes &&
                   patches == other.patches;
        }
    }
    
    public abstract class EntityChanges { }
    
    public class EntityChanges<T> : EntityChanges where T : class {
        public              ChangeInfo<T>                       Info { get; }
        // ReSharper disable once InconsistentNaming
        internal readonly   EntityPeerSet<T>                    _set;
        
        public   readonly   Dictionary<string, T>               creates = new Dictionary<string, T>();
        public   readonly   Dictionary<string, T>               updates = new Dictionary<string, T>();
        public   readonly   HashSet   <string>                  deletes = new HashSet   <string>();
        public   readonly   Dictionary<string, ChangePatch<T>>  patches = new Dictionary<string, ChangePatch<T>>();
        
        public override     string                              ToString() => Info.ToString();       

        internal EntityChanges(EntityPeerSet<T> set) {
            this._set = set;
            Info = new ChangeInfo<T>();
        }

        internal void Clear() {
            creates.Clear();
            updates.Clear();
            deletes.Clear();
            patches.Clear();
            //
            Info.Clear();
        }
    }
    
    public readonly struct ChangePatch<T> where T : class {
        public  readonly    T                   entity;
        public  readonly    List<JsonPatch>     patches;

        public  override    string              ToString() => StaticEntityId.GetId(entity);
        
        private static readonly   EntityId<T>   StaticEntityId = EntityId.GetEntityId<T>();


        public ChangePatch(T entity, List<JsonPatch> patches) {
            this.entity     = entity;
            this.patches    = patches;
        }
    }
}