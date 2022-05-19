// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Client
{
    public class SubscriptionProcessor : IDisposable
    {
        private readonly    Dictionary<Type, EntityChanges>     results   = new Dictionary<Type, EntityChanges>();
        private readonly    List<Message>                       messages  = new List<Message>();
        private             ObjectMapper                        messageMapper;
        public              int                                 EventSequence { get; private set ; }
        public override     string                              ToString() => $"EventSequence: {EventSequence}";

        public              List<Message>                       GetMessages() => messages;

        public EntityChanges<TKey, T> GetEntityChanges<TKey, T>(EntitySet<TKey, T> entitySet) where T : class {
            return (EntityChanges<TKey, T>)GetChanges(entitySet);
        }

        public virtual void OnEvent(FlioxClient client, EventMessage ev) {
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
            foreach (var result in results) {
                result.Value.Clear();
            }
            EventSequence++;
            using (var pooled = client.ObjectMapper.Get()) {
                var mapper = pooled.instance;
                var logger = client.Logger;
                foreach (var task in ev.tasks) {
                    switch (task.TaskType)
                    {
                        case TaskType.create:   ProcessCreate (client, (CreateEntities)task, mapper);   break;
                        case TaskType.upsert:   ProcessUpsert (client, (UpsertEntities)task, mapper);   break;
                        case TaskType.delete:   ProcessDelete (client, (DeleteEntities)task);           break;
                        case TaskType.patch:    ProcessPatch  (client, (PatchEntities) task, mapper);   break;
                        case TaskType.message:
                        case TaskType.command:
                            ProcessMessage (client,(SyncMessageTask)task, messageMapper, logger);
                            break;
                    }
                }
            }
        }
        
        private void ProcessCreate(FlioxClient client, CreateEntities create, ObjectMapper mapper) {
            var set = client.GetEntitySet(create.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
            create.entityKeys = GetKeysFromEntities (client, set.GetKeyName(), create.entities);
            SyncPeerEntities(set, create.entityKeys, create.entities, mapper);
                            
            // --- update changes
            var changes = GetChanges(set);
            foreach (var id in create.entityKeys) {
                changes.AddCreate(id);
            }
        }
        
        private void ProcessUpsert(FlioxClient client, UpsertEntities upsert, ObjectMapper mapper) {
            var set = client.GetEntitySet(upsert.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
            upsert.entityKeys = GetKeysFromEntities (client, set.GetKeyName(), upsert.entities);
            SyncPeerEntities(set, upsert.entityKeys, upsert.entities, mapper);
                            
            // --- update changes
            var changes = GetChanges(set);
            foreach (var id in upsert.entityKeys) {
                changes.AddUpsert(id);
            }
        }
        
        private void ProcessDelete(FlioxClient client, DeleteEntities delete) {
            var set = client.GetEntitySet(delete.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
            set.DeletePeerEntities (delete.ids);
                            
            // --- update changes
            var changes = GetChanges(set);
            foreach (var id in delete.ids) {
                changes.AddDelete(id);
            }
        }
        
        private void ProcessPatch(FlioxClient client, PatchEntities patches, ObjectMapper mapper) {
            var set = client.GetEntitySet(patches.container);
            // apply changes only if subscribed
            if (set.GetSubscription() == null)
                return;
            set.PatchPeerEntities(patches.patches, mapper);
                            
            // --- update changes
            var changes = GetChanges(set);
            foreach (var pair in patches.patches) {
                var id      = pair.Key;
                var patch   = pair.Value;
                changes.AddPatch(id, patch);
            }
        }
        
        private void ProcessMessage(FlioxClient client, SyncMessageTask task, ObjectMapper mapper, IHubLogger logger) {
            var name = task.name;
            // callbacks require their own reader as store._intern.jsonMapper.reader cannot be used.
            // This jsonMapper is used in various threads caused by .ConfigureAwait(false) continuations
            // and ProcessEvent() can be called concurrently from the 'main' thread.
            var invokeContext = new InvokeContext(name, task.param, mapper.reader, logger);
            if (client._intern.subscriptions.TryGetValue(name, out MessageSubscriber subscriber)) {
                subscriber.InvokeCallbacks(invokeContext);    
            }
            foreach (var sub in client._intern.subscriptionsPrefix) {
                if (name.StartsWith(sub.name)) {
                    sub.InvokeCallbacks(invokeContext);
                }
            }
            var message         = new Message(invokeContext);
            messages.Add(message);
        }
        
        private static List<JsonKey> GetKeysFromEntities(FlioxClient client, string keyName, List<JsonValue> entities) {
            var processor = client._intern.EntityProcessor();
            var keys = new List<JsonKey>(entities.Count);
            foreach (var entity in entities) {
                if (!processor.GetEntityKey(entity, keyName, out JsonKey key, out string error))
                    throw new InvalidOperationException($"CreateEntityKeys() error: {error}");
                keys.Add(key);
            }
            return keys;
        }
        
        private static void SyncPeerEntities (EntitySet set, List<JsonKey> keys, List<JsonValue> entities, ObjectMapper mapper) {
            if (keys.Count != entities.Count)
                throw new InvalidOperationException("Expect equal counts");
            var syncEntities = new Dictionary<JsonKey, EntityValue>(entities.Count, JsonKey.Equality);
            for (int n = 0; n < entities.Count; n++) {
                var entity  = entities[n];
                var key     = keys[n];
                var value = new EntityValue(entity);
                syncEntities.Add(key, value);
            }
            set.SyncPeerEntities(syncEntities, mapper);
        }
        
        private EntityChanges GetChanges (EntitySet entitySet) {
            var entityType = entitySet.EntityType;
            if (results.TryGetValue(entityType, out var result))
                return result;
            object[] constructorParams = { entitySet };
            var keyType     = entitySet.KeyType;
            var instance    = TypeMapperUtils.CreateGenericInstance(typeof(EntityChanges<,>), new[] {keyType, entityType}, constructorParams);
            result          = (EntityChanges)instance;
            results.Add(entityType, result);
            return result;
        }
    }
    
    public sealed class ChangeInfo<T> : ChangeInfo where T : class
    {
        public bool IsEqual(ChangeInfo<T> other) {
            return creates == other.creates &&
                   upserts == other.upserts &&
                   deletes == other.deletes &&
                   patches == other.patches;
        }
    }
    
    public abstract class EntityChanges
    {
        internal abstract void Clear        ();
        internal abstract void AddCreate    (in JsonKey id);
        internal abstract void AddUpsert    (in JsonKey id);
        internal abstract void AddDelete    (in JsonKey id);
        internal abstract void AddPatch     (in JsonKey id, EntityPatch entityPatch);
    }
    
    public sealed class EntityChanges<TKey, T> : EntityChanges where T : class {
        public              ChangeInfo<T>                       Info { get; }
        private  readonly   EntitySet<TKey, T>                  entitySet;
        
        public   readonly   Dictionary<TKey, T>                 creates = SyncSet.CreateDictionary<TKey, T>();
        public   readonly   Dictionary<TKey, T>                 upserts = SyncSet.CreateDictionary<TKey, T>();
        public   readonly   HashSet   <TKey>                    deletes = SyncSet.CreateHashSet<TKey>();
        public   readonly   Dictionary<TKey, ChangePatch<T>>    patches = SyncSet.CreateDictionary<TKey, ChangePatch<T>>();
        
        public override     string                              ToString() => Info.ToString();       

        internal EntityChanges(EntitySet<TKey, T> entitySet) {
            this.entitySet = entitySet;
            Info = new ChangeInfo<T>();
        }

        internal override void Clear() {
            creates.Clear();
            upserts.Clear();
            deletes.Clear();
            patches.Clear();
            //
            Info.Clear();
        }
        
        internal override void AddCreate(in JsonKey id) {
            TKey    key     = Ref<TKey,T>.RefKeyMap.IdToKey(id);
            var     peer    = entitySet.GetOrCreatePeerByKey(key, id);
            var     entity  = peer.Entity;
            creates.Add(key, entity);
            Info.creates++;
        }
        
        internal override void AddUpsert(in JsonKey id) {
            TKey    key     = Ref<TKey,T>.RefKeyMap.IdToKey(id);
            var     peer    = entitySet.GetOrCreatePeerByKey(key, id);
            var     entity  = peer.Entity;
            upserts.Add(key, entity);
            Info.upserts++;
        }
        
        internal override void AddDelete(in JsonKey id) {
            TKey    key      = Ref<TKey,T>.RefKeyMap.IdToKey(id);
            deletes.Add(key);
            Info.deletes++;
        }
        
        internal override void AddPatch(in JsonKey id, EntityPatch entityPatch) {
            TKey        key         = Ref<TKey,T>.RefKeyMap.IdToKey(id);
            var         peer        = entitySet.GetOrCreatePeerByKey(key, id);
            var         entity      = peer.Entity;
            var         changePatch = new ChangePatch<T>(entity, entityPatch.patches);
            patches.Add(key, changePatch);
            Info.patches++;
        }
    }
    
    public readonly struct ChangePatch<T> where T : class {
        public  readonly    T                   entity;
        public  readonly    List<JsonPatch>     patches;

        public  override    string              ToString() => EntitySetBase<T>.EntityKeyMap.GetId(entity).AsString();
        
        public ChangePatch(T entity, List<JsonPatch> patches) {
            this.entity     = entity;
            this.patches    = patches;
        }
    }
}