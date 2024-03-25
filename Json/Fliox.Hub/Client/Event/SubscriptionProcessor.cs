// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Used to process <see cref="SyncEvent"/>'s received by a <see cref="FlioxClient"/>.  
    /// </summary>
    public sealed class SubscriptionProcessor : IDisposable
    {
        private  readonly   EventContext                    eventContext;
        private  readonly   Dictionary<ShortString,Changes> changes         = new Dictionary<ShortString,Changes>(ShortString.Equality);
        /// <summary> contain only <see cref="Changes"/> where <see cref="Changes.Count"/> > 0 </summary>
        internal readonly   List<Changes>                   contextChanges  = new List<Changes>();
        internal readonly   List<Message>                   messages        = new List<Message>();
        private  readonly   SubscriptionIntern              intern;
        internal            int                             EventCount { get; private set ; }
        private  readonly   List<MessageCallback>           tempCallbackHandlers    = new List<MessageCallback>();
        private  readonly   List<MessageSubscriber>         tempSubscriptionsPrefix = new List<MessageSubscriber>();
        
        public   override   string                          ToString()  => $"EventCount: {EventCount}";
        
        public SubscriptionProcessor() {
            eventContext    = new EventContext(this);
            intern          = new SubscriptionIntern();
        }

        public void Dispose() {
            intern.objectMapper?.Dispose();
        }

        /// <summary>
        /// Process the <see cref="SyncEvent.tasks"/> of the given <see cref="SyncEvent"/>.
        /// These <see cref="SyncEvent.tasks"/> are "messages" resulting from subscriptions registered by
        /// methods like <see cref="EntitySet{TKey,T}.SubscribeChanges"/>, <see cref="FlioxClient.SubscribeAllChanges"/> or
        /// <see cref="FlioxClient.SubscribeMessage"/>.
        /// </summary>
        public void ProcessEvent(FlioxClient client, in SyncEvent syncEvent, int seq) {
            eventContext.Init(client, syncEvent, seq);
            if (client._intern.disposed)  // store may already be disposed
                return;
            if (intern.objectMapper == null) {
                // use individual ObjectMapper for messages as they are used by App outside the pooled scope below
                intern.objectMapper = new ObjectMapper(client._readonly.typeStore);
                intern.objectMapper.ErrorHandler = ObjectReader.NoThrow;
            }
            messages.Clear();
            // clear all changes from the last event
            foreach (var change in contextChanges) {
                change.Clear();
            }
            contextChanges.Clear();
            EventCount++;
            var db = syncEvent.db;
            if (!db.IsNull() && !client._readonly.databaseShort.IsEqual(db)) {
                var msg = $"invalid SyncEvent db: {db}. expect: {client.DatabaseName}";
                eventContext.Logger.Log(HubLog.Error, msg);
                return;
            }
            foreach (var task in syncEvent.tasks) {
                switch (task.TaskType)
                {
                    case TaskType.create:   ProcessCreate (client, (CreateEntities) task);  break;
                    case TaskType.upsert:   ProcessUpsert (client, (UpsertEntities) task);  break;
                    case TaskType.delete:   ProcessDelete (client, (DeleteEntities) task);  break;
                    case TaskType.merge:    ProcessPatch  (client, (MergeEntities)  task);  break;
                    case TaskType.message:
                    case TaskType.command:  ProcessMessage(        (SyncMessageTask)task);  break;
                }
            }
            // After processing event message invoke their handler methods:
            
            // --- invoke subscription event handler
            client._intern.subscriptionEventHandler?.Invoke(eventContext);
            
            // --- invoke changes handlers
            foreach (var change in contextChanges) {
                client.TryGetSetByName(change.ContainerShort, out Set set);
                set.changeCallback?.InvokeCallback(change, eventContext);
            }
            if (contextChanges.Count > 0) {
                client._intern.changeSubscriptionHandler?.Invoke(eventContext);
            }
            
            // --- invoke message handlers
            foreach (var message in messages) {
                var subs = client._intern.subscriptions;
                if (subs != null && subs.TryGetValue(message.invokeContext.name, out MessageSubscriber subscriber)) {
                    subscriber.InvokeCallbacks(message.invokeContext, eventContext);    
                }
                var subsPrefix = client._intern.subscriptionsPrefix;
                if (subsPrefix != null) {
                    tempSubscriptionsPrefix.Clear();
                    tempSubscriptionsPrefix.AddRange(subsPrefix);
                    foreach (var sub in tempSubscriptionsPrefix) {
                        if (message.invokeContext.name.StartsWith(sub.name)) {
                            sub.InvokeCallbacks(message.invokeContext, eventContext);
                        }
                    }
                }
            }
        }
        
        private static void AddEntitiesToValues(List<JsonEntity> entities, List<JsonValue> values) {
            foreach (var entity in entities) {
                values.Add(entity.value);
            }
        }
        
        private void ProcessCreate(FlioxClient client, CreateEntities create) {
            var entities = create.entities;
            if (entities.Count == 0)
                return;
            var set = client.GetEntitySet(create.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            AddEntitiesToValues(entities, entityChanges.raw.creates);
            entityChanges.changeInfo.creates += entities.Count;
        }
        
        private void ProcessUpsert(FlioxClient client, UpsertEntities upsert) {
            var entities = upsert.entities;
            if (entities.Count == 0)
                return;
            var set = client.GetEntitySet(upsert.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            AddEntitiesToValues(entities, entityChanges.raw.upserts);
            entityChanges.changeInfo.upserts += entities.Count;
        }
        
        private void ProcessDelete(FlioxClient client, DeleteEntities delete) {
            var ids = delete.ids;
            if (ids.Count == 0)
                return;
            var set = client.GetEntitySet(delete.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            entityChanges.raw.deletes.AddRange(ids);
            entityChanges.changeInfo.deletes += ids.Count;
        }
        
        private void ProcessPatch(FlioxClient client, MergeEntities patchEntities) {
            var patches = patchEntities.patches;
            if (patches.Count == 0)
                return;
            var set = client.GetEntitySet(patchEntities.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            AddEntitiesToValues(patches, entityChanges.raw.patches);
            entityChanges.changeInfo.merges += patches.Count;
        }
        
        private void ProcessMessage(SyncMessageTask task) {
            // callbacks require their own reader as store._intern.jsonMapper.reader cannot be used.
            // This jsonMapper is used in various threads caused by .ConfigureAwait(false) continuations
            // and ProcessEvent() can be called concurrently from the 'main' thread.
            var invokeContext   = new InvokeContext(task.name, task.param, intern.objectMapper.reader, tempCallbackHandlers);
            var message         = new Message(invokeContext);
            messages.Add(message);
        }
        
        private void AddChanges(Changes entityChanges) {
            if (entityChanges.added)
                return;
            contextChanges.Add(entityChanges);
            entityChanges.added = true;
        }
        
        internal Changes GetChanges (Set entitySet) {
            if (changes.TryGetValue(entitySet.nameShort, out var change))
                return change;
            object[] constructorParams = { entitySet, intern };
            var keyType     = entitySet.KeyType;
            var entityType  = entitySet.EntityType;
            var genericArgs = new[] { keyType, entityType };
            var instance    = TypeMapperUtils.CreateGenericInstance(typeof(Changes<,>), genericArgs, constructorParams);
            change          = (Changes)instance;
            changes.Add(entitySet.nameShort, change);
            return change;
        }
    }
}