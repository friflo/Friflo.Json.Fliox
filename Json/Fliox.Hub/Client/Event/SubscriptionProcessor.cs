// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

// internal Event API => use separate namespace
namespace Friflo.Json.Fliox.Hub.Client.Event
{
    internal sealed class SubscriptionProcessor : IDisposable
    {
        private  readonly   Dictionary<Type, Changes>   changes         = new Dictionary<Type, Changes>();
        /// <summary> contain only <see cref="Changes"/> where <see cref="Changes.Count"/> > 0 </summary>
        internal readonly   List<Changes>               contextChanges  = new List<Changes>();
        internal readonly   List<Message>               messages        = new List<Message>();
        private             ObjectMapper                messageMapper;
        internal            int                         EventSequence { get; private set ; }
        
        public   override   string                      ToString()  => $"EventSequence: {EventSequence}";

        public void Dispose() {
            messageMapper?.Dispose();
        }

        /// <summary>
        /// Process the <see cref="EventMessage.tasks"/> of the given <see cref="EventMessage"/>.
        /// These <see cref="EventMessage.tasks"/> are "messages" resulting from subscriptions registered by
        /// methods like <see cref="EntitySet{TKey,T}.SubscribeChanges"/>, <see cref="FlioxClient.SubscribeAllChanges"/> or
        /// <see cref="FlioxClient.SubscribeMessage"/>.
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

            foreach (var task in ev.tasks) {
                switch (task.TaskType)
                {
                    case TaskType.create:   ProcessCreate (client, (CreateEntities)task);   break;
                    case TaskType.upsert:   ProcessUpsert (client, (UpsertEntities)task);   break;
                    case TaskType.delete:   ProcessDelete (client, (DeleteEntities)task);   break;
                    case TaskType.patch:    ProcessPatch  (client, (PatchEntities) task);   break;
                    case TaskType.message:
                    case TaskType.command:  ProcessMessage ((SyncMessageTask)task, messageMapper);  break;
                }
            }
            // After processing / collecting all change & message tasks invoke their handler methods
            // --- prepare EventContext state
            var logger          = client.Logger;
            var eventContext    = new EventContext(this, ev, logger);
            contextChanges.Clear();
            foreach (var change in changes) {
                Changes entityChanges = change.Value;
                if (entityChanges.Count == 0)
                    continue;
                contextChanges.Add(entityChanges);
            }
            
            // --- invoke subscription event handler
            client._intern.subscriptionEventHandler?.Invoke(eventContext);
            
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
        
        private void ProcessCreate(FlioxClient client, CreateEntities create) {
            var set = client.GetEntitySet(create.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            entityChanges.AddCreates(create.entities);
        }
        
        private void ProcessUpsert(FlioxClient client, UpsertEntities upsert) {
            var set = client.GetEntitySet(upsert.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            entityChanges.AddUpserts(upsert.entities);
        }
        
        private void ProcessDelete(FlioxClient client, DeleteEntities delete) {
            var set = client.GetEntitySet(delete.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            entityChanges.AddDeletes(delete.ids);
        }
        
        private void ProcessPatch(FlioxClient client, PatchEntities patches) {
            var set = client.GetEntitySet(patches.container);
            if (set.GetSubscription() == null) {
                return;
            }
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
        
        internal Changes GetChanges (EntitySet entitySet) {
            var entityType = entitySet.EntityType;
            if (changes.TryGetValue(entityType, out var change))
                return change;
            object[] constructorParams = { entitySet, messageMapper };
            var keyType     = entitySet.KeyType;
            var genericArgs = new[] { keyType, entityType };
            var instance    = TypeMapperUtils.CreateGenericInstance(typeof(Changes<,>), genericArgs, constructorParams);
            change          = (Changes)instance;
            changes.Add(entityType, change);
            return change;
        }
    }
}