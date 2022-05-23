// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void ChangeSubscriptionHandler         (EventContext context);
    public delegate void ChangeSubscriptionHandler<TKey, T>(EntityChanges<TKey, T> change, EventContext context) where T : class;
    
    public abstract class EntityChanges
    {
        public              int             Count       => ChangeInfo.Count;
        public              ChangeInfo      ChangeInfo  { get; } = new ChangeInfo();
        public    abstract  string          Container   { get; }
        
        internal  abstract  Type            GetEntityType();
        internal  abstract  void            Clear       ();
        internal  abstract  void            AddCreates  (List<JsonValue> entities, ObjectMapper mapper);
        internal  abstract  void            AddUpserts  (List<JsonValue> entities, ObjectMapper mapper);
        internal  abstract  void            AddDeletes  (HashSet<JsonKey> ids);
        internal  abstract  void            AddPatches  (Dictionary<JsonKey, EntityPatch> patches);
    }
    
    public sealed class EntityChanges<TKey, T> : EntityChanges where T : class
    {
        // used properties for Creates, Upserts, Deletes & Patches to enable changing implementation. May fill these properties lazy in future.
        public              List<T>                 Creates { get; } = new List<T>();
        public              List<T>                 Upserts { get; } = new List<T>();
        public              HashSet<TKey>           Deletes { get; } = SyncSet.CreateHashSet<TKey>();
        public              List<ChangePatch<T>>    Patches { get; } = new List<ChangePatch<T>>();
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private  readonly   EntitySet<TKey, T>      entitySet;
        
        public   override   string                  ToString()  => ChangeInfo.ToString();       
        public   override   string                  Container   { get; }
        internal override   Type                    GetEntityType() => typeof(T);

        internal EntityChanges(EntitySet<TKey, T> entitySet) {
            this.entitySet  = entitySet;

            Container       = entitySet.name;
        }
        
        internal override void Clear() {
            Creates.Clear();
            Upserts.Clear();
            Deletes.Clear();
            Patches.Clear();
            //
            ChangeInfo.Clear();
        }
        
        internal override void AddCreates (List<JsonValue> entities, ObjectMapper mapper) {
            foreach (var entity in entities) {
                var value = mapper.Read<T>(entity);
                Creates.Add(value);
            }
            ChangeInfo.creates += entities.Count;
        }
        
        internal override void AddUpserts (List<JsonValue> entities, ObjectMapper mapper) {
            foreach (var entity in entities) {
                var value = mapper.Read<T>(entity);
                Upserts.Add(value);
            }
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
                var     peer        = entitySet.GetOrCreatePeerByKey(key, id); // todo remove access to entitySet
                var     entity      = peer.Entity;
                var     changePatch = new ChangePatch<T>(entity, entityPatch.patches);
                Patches.Add(changePatch);
            }
            ChangeInfo.patches += entityPatches.Count;
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
    
   
    internal abstract class ChangeCallback {
        internal abstract void InvokeCallback(EntityChanges entityChanges, EventContext context);
    }
    
    internal sealed class GenericChangeCallback<TKey, T> : ChangeCallback where T : class
    {
        private  readonly   ChangeSubscriptionHandler<TKey, T>   handler;
        
        internal GenericChangeCallback (ChangeSubscriptionHandler<TKey, T> handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(EntityChanges entityChanges, EventContext context) {
            var changes = (EntityChanges<TKey,T>)entityChanges;
            handler(changes, context);
        }
    }
}