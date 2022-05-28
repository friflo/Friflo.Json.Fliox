// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void ChangeSubscriptionHandler         (EventContext context);
    public delegate void ChangeSubscriptionHandler<TKey, T>(Changes<TKey, T> changes, EventContext context) where T : class;
    
    public abstract class Changes
    {
        public              int             Count       => ChangeInfo.Count;
        public              ChangeInfo      ChangeInfo  { get; } = new ChangeInfo();
        public    abstract  string          Container   { get; }
        
        [DebuggerBrowsable(Never)] protected readonly   List<JsonValue> rawCreates = new List<JsonValue>();
        [DebuggerBrowsable(Never)] protected readonly   List<JsonValue> rawUpserts = new List<JsonValue>();

        internal  abstract  Type    GetEntityType();
        internal  abstract  void    Clear       ();
        internal  abstract  void    AddCreates  (List<JsonValue> entities, ObjectMapper mapper);
        internal  abstract  void    AddUpserts  (List<JsonValue> entities, ObjectMapper mapper);
        internal  abstract  void    AddDeletes  (HashSet<JsonKey> ids);
        internal  abstract  void    AddPatches  (Dictionary<JsonKey, EntityPatch> patches);
        
        internal  abstract  void    ApplyChangesTo  (EntitySet entitySet);
    }
    
    public sealed class Changes<TKey, T> : Changes where T : class
    {
        // used properties for Creates, Upserts, Deletes & Patches to enable changing implementation. May fill these properties lazy in future.
        public              List<T>             Creates { get; } = new List<T>();
        public              List<T>             Upserts { get; } = new List<T>();
        public              HashSet<TKey>       Deletes { get; } = SyncSet.CreateHashSet<TKey>();
        public              List<Patch<TKey>>   Patches { get; } = new List<Patch<TKey>>();
        
        public   override   string              ToString()      => ChangeInfo.ToString();       
        public   override   string              Container       { get; }
        internal override   Type                GetEntityType() => typeof(T);

        /// <summary> called via <see cref="Event.SubscriptionProcessor.GetChanges"/> </summary>
        internal Changes(EntitySet<TKey, T> entitySet) {
            Container       = entitySet.name;
        }
        
        internal override void Clear() {
            Creates.Clear();
            Upserts.Clear();
            Deletes.Clear();
            Patches.Clear();
            
            rawCreates.Clear();
            rawUpserts.Clear();
            //
            ChangeInfo.Clear();
        }
        
        internal override void AddCreates (List<JsonValue> entities, ObjectMapper mapper) {
            foreach (var entity in entities) {
                var value = mapper.Read<T>(entity);
                Creates.Add(value);
            }
            rawCreates.AddRange(entities);
            ChangeInfo.creates += entities.Count;
        }
        
        internal override void AddUpserts (List<JsonValue> entities, ObjectMapper mapper) {
            foreach (var entity in entities) {
                var value = mapper.Read<T>(entity);
                Upserts.Add(value);
            }
            rawUpserts.AddRange(entities);
            ChangeInfo.upserts += entities.Count;
        }
        
        internal override void AddDeletes  (HashSet<JsonKey> ids) {
            foreach (var id in ids) {
                TKey    key      = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                Deletes.Add(key);
            }
            ChangeInfo.deletes += ids.Count;
        }
        
        internal override void AddPatches(Dictionary<JsonKey, EntityPatch> entityPatches) {
            foreach (var pair in entityPatches) {
                var     id          = pair.Key;
                var     entityPatch = pair.Value;
                TKey    key         = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                var     patch       = new Patch<TKey>(key, id, entityPatch.patches);
                Patches.Add(patch);
            }
            ChangeInfo.patches += entityPatches.Count;
        }
        
        internal override void ApplyChangesTo  (EntitySet entitySet) {
            var set = (EntitySet<TKey, T>)entitySet;
            ApplyChangesTo(set);
        }
        
        public void ApplyChangesTo(EntitySet<TKey, T> entitySet) {
            if (Count == 0)
                return;
            var client = entitySet.intern.store;
            using (var pooled = client._intern.pool.ObjectMapper.Get()) {
                var mapper = pooled.instance;
                if (rawCreates.Count > 0) {
                    var entityKeys = GetKeysFromEntities (client, entitySet.GetKeyName(), rawCreates);
                    SyncPeerEntities(entitySet, entityKeys, rawCreates, mapper);
                }
                if (rawUpserts.Count > 0) {
                    var entityKeys = GetKeysFromEntities (client, entitySet.GetKeyName(), rawUpserts);
                    SyncPeerEntities(entitySet, entityKeys, rawUpserts, mapper);
                }
                entitySet.PatchPeerEntities(Patches, mapper);
            }
            entitySet.DeletePeerEntities(Deletes);
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
            // todo simplify - creating a Dictionary<,> is overkill
            set.SyncPeerEntities(syncEntities, mapper);
        }
    }
    
    public readonly struct Patch<TKey> {
        public    readonly  TKey                key;
        public    readonly  List<JsonPatch>     patches;
        
        internal  readonly  JsonKey             id;

        public  override    string              ToString() => key.ToString();
        
        public Patch(TKey key, in JsonKey id, List<JsonPatch> patches) {
            this.id         = id;
            this.key        = key;
            this.patches    = patches;
        }
    }
    

    internal abstract class ChangeCallback {
        internal abstract void InvokeCallback(Changes entityChanges, EventContext context);
    }
    
    internal sealed class GenericChangeCallback<TKey, T> : ChangeCallback where T : class
    {
        private  readonly   ChangeSubscriptionHandler<TKey, T>   handler;
        
        internal GenericChangeCallback (ChangeSubscriptionHandler<TKey, T> handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(Changes entityChanges, EventContext context) {
            var changes = (Changes<TKey,T>)entityChanges;
            handler(changes, context);
        }
    }
}