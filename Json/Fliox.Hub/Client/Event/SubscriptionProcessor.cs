// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

// SubscriptionProcessor is commonly not used directly by application => use separate namespace
namespace Friflo.Json.Fliox.Hub.Client.Event
{
    internal sealed class SubscriptionProcessor : IDisposable
    {
        private  readonly   Dictionary<Type, EntityChanges> changes         = new Dictionary<Type, EntityChanges>();
        /// <summary> contain only <see cref="EntityChanges"/> where <see cref="EntityChanges.Count"/> > 0 </summary>
        internal readonly   List<EntityChanges>             contextChanges  = new List<EntityChanges>();
        internal readonly   List<Message>                   messages        = new List<Message>();
        private             ObjectMapper                    messageMapper;
        internal            int                             EventSequence { get; private set ; }
        
        public   override   string                          ToString()  => $"EventSequence: {EventSequence}";

        public void OnEvent(FlioxClient client, EventMessage ev) {
            ProcessEvent(client, ev);
        }
        
        public void Dispose() {
            messageMapper?.Dispose();
        }

        /// <summary>
        /// Process the <see cref="EventMessage.tasks"/> of the given <see cref="EventMessage"/>.
        /// These <see cref="EventMessage.tasks"/> are "messages" resulting from subscriptions registered by
        /// methods like <see cref="EntitySet{TKey,T}.SubscribeChanges"/>, <see cref="FlioxClient.SubscribeAllChanges"/> or
        /// <see cref="FlioxClient.SubscribeMessage"/>.
        /// <br></br>
        /// Tasks notifying about database changes are applied to the <see cref="FlioxClient"/> the <see cref="SubscriptionProcessor"/>
        /// is attached to.
        /// Types of database changes refer to <see cref="Change.create"/>ed, <see cref="Change.upsert"/>ed,
        /// <see cref="Change.delete"/>ed and <see cref="Change.patch"/>ed entities.
        /// <br></br>
        /// Tasks notifying "messages" are ignored. These message subscriptions are registered by <see cref="FlioxClient.SubscribeMessage"/>.
        /// </summary>
        public void ProcessEvent(FlioxClient client, EventMessage ev) {
            if (client._intern.disposed)  // store may already be disposed
                return;
            if (messageMapper == null) {
                // use individual ObjectMapper for messages as they are used by App outside the pooled scope bellow
                messageMapper = new ObjectMapper(client._intern.typeStore);
                messageMapper.ErrorHandler = ObjectReader.NoThrow;
            }
            messages.Clear();
            foreach (var change in changes) {
                change.Value.Clear();
            }
            EventSequence++;
            using (var pooled = client.ObjectMapper.Get()) {
                var mapper = pooled.instance;
                foreach (var task in ev.tasks) {
                    switch (task.TaskType)
                    {
                        case TaskType.create:   ProcessCreate (client, (CreateEntities)task, mapper);   break;
                        case TaskType.upsert:   ProcessUpsert (client, (UpsertEntities)task, mapper);   break;
                        case TaskType.delete:   ProcessDelete (client, (DeleteEntities)task);           break;
                        case TaskType.patch:    ProcessPatch  (client, (PatchEntities) task, mapper);   break;
                        case TaskType.message:
                        case TaskType.command:  ProcessMessage ((SyncMessageTask)task, messageMapper);  break;
                    }
                }
            }
            // After processing / collecting all change & message tasks invoke their handler methods
            // --- prepare EventContext state
            var logger          = client.Logger;
            var eventContext    = new EventContext(this, ev, logger);
            contextChanges.Clear();
            foreach (var change in changes) {
                EntityChanges entityChanges = change.Value;
                if (entityChanges.Count == 0)
                    continue;
                contextChanges.Add(entityChanges);
            }
            // --- invoke subscription handler
            client._intern.subscriptionHandler?.Invoke(eventContext);
            
            // --- invoke changes handlers
            foreach (var change in contextChanges) {
                var entityType = change.GetEntityType();
                client._intern.TryGetSetByType(entityType, out EntitySet set);
                set.changeCallback?.InvokeCallback(change, eventContext);
            }
            if (contextChanges.Count > 0) {
                client._intern.changeSubscriptionHandler?.Invoke(eventContext);
            }
            
            // --- invoke message handlers
            foreach (var message in messages) {
                var name = message.Name;
                if (client._intern.subscriptions.TryGetValue(name, out MessageSubscriber subscriber)) {
                    subscriber.InvokeCallbacks(message.invokeContext, eventContext);    
                }
                foreach (var sub in client._intern.subscriptionsPrefix) {
                    if (name.StartsWith(sub.name)) {
                        sub.InvokeCallbacks(message.invokeContext, eventContext);
                    }
                }
            }
        }
        
        private void ProcessCreate(FlioxClient client, CreateEntities create, ObjectMapper mapper) {
            var set = client.GetEntitySet(create.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
                            
            // --- update changes
            var entityChanges = GetChanges(set);
            entityChanges.AddCreates(create.entities, mapper);
        }
        
        private void ProcessUpsert(FlioxClient client, UpsertEntities upsert, ObjectMapper mapper) {
            var set = client.GetEntitySet(upsert.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
                            
            // --- update changes
            var entityChanges = GetChanges(set);
            entityChanges.AddUpserts(upsert.entities, mapper);
        }
        
        private void ProcessDelete(FlioxClient client, DeleteEntities delete) {
            var set = client.GetEntitySet(delete.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
                            
            // --- update changes
            var entityChanges = GetChanges(set);
            entityChanges.AddDeletes(delete.ids);
        }
        
        private void ProcessPatch(FlioxClient client, PatchEntities patches, ObjectMapper mapper) {
            var set = client.GetEntitySet(patches.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
                            
            // --- update changes
            var entityChanges = GetChanges(set);
            entityChanges.AddPatches(patches.patches);
        }
        
        private void ProcessMessage(SyncMessageTask task, ObjectMapper mapper) {
            var name = task.name;
            // callbacks require their own reader as store._intern.jsonMapper.reader cannot be used.
            // This jsonMapper is used in various threads caused by .ConfigureAwait(false) continuations
            // and ProcessEvent() can be called concurrently from the 'main' thread.
            var invokeContext   = new InvokeContext(name, task.param, mapper.reader);
            var message         = new Message(invokeContext);
            messages.Add(message);
        }
        
        internal EntityChanges GetChanges (EntitySet entitySet) {
            var entityType = entitySet.EntityType;
            if (changes.TryGetValue(entityType, out var change))
                return change;
            object[] constructorParams = { entitySet };
            var keyType     = entitySet.KeyType;
            var instance    = TypeMapperUtils.CreateGenericInstance(typeof(EntityChanges<,>), new[] {keyType, entityType}, constructorParams);
            change          = (EntityChanges)instance;
            changes.Add(entityType, change);
            return change;
        }
    }
}