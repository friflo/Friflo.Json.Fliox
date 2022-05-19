// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
    public abstract class EntityChanges
    {
        internal  abstract  int     Count       ();
        internal  abstract  void    Clear       ();
        internal  abstract  void    AddCreate   (in JsonKey id);
        internal  abstract  void    AddUpsert   (in JsonKey id);
        internal  abstract  void    AddDelete   (in JsonKey id);
        internal  abstract  void    AddPatch    (in JsonKey id, EntityPatch entityPatch);
    }
    
    public sealed class EntityChanges<TKey, T> : EntityChanges where T : class {
        public              ChangeInfo<T>                       Info { get; }
        
        public   readonly   Dictionary<TKey, T>                 creates = SyncSet.CreateDictionary<TKey, T>();
        public   readonly   Dictionary<TKey, T>                 upserts = SyncSet.CreateDictionary<TKey, T>();
        public   readonly   HashSet   <TKey>                    deletes = SyncSet.CreateHashSet<TKey>();
        public   readonly   Dictionary<TKey, ChangePatch<T>>    patches = SyncSet.CreateDictionary<TKey, ChangePatch<T>>();
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private  readonly   EntitySet<TKey, T>                  entitySet;
        
        public   override   string                              ToString    () => Info.ToString();       
        internal override   int                                 Count       () => Info.Count;

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
    
    public sealed class ChangeInfo<T> : ChangeInfo where T : class
    {
        public bool IsEqual(ChangeInfo<T> other) {
            return creates == other.creates &&
                   upserts == other.upserts &&
                   deletes == other.deletes &&
                   patches == other.patches;
        }
    }
    
    internal abstract class ChangeCallback {
        internal abstract void InvokeCallback(EntityChanges entityChanges);
    }
    
    internal sealed class GenericChangeCallback<TKey, T> : ChangeCallback where T : class
    {
        private  readonly   ChangeSubscriptionHandler<TKey, T>   handler;
        
        internal GenericChangeCallback (ChangeSubscriptionHandler<TKey, T> handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(EntityChanges entityChanges) {
            var changes = (EntityChanges<TKey,T>)entityChanges;
            handler(changes);
        }
    }
}