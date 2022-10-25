// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// EntitySet & EntitySetBase<T> are not intended as a public API.
// These classes are declared here to simplify navigation to EntitySet<TKey, T>.
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        [DebuggerBrowsable(Never)] public   readonly  string          name;
        [DebuggerBrowsable(Never)] internal           ChangeCallback  changeCallback;

        internal  abstract  SyncSet     SyncSet     { get; }
        internal  abstract  SetInfo     SetInfo     { get; }
        internal  abstract  Type        KeyType     { get; }
        internal  abstract  Type        EntityType  { get; }
        public    abstract  bool        WritePretty { get; set; }
        public    abstract  bool        WriteNull   { get; set; }
        
        internal  abstract  void                Init                    (FlioxClient store);
        internal  abstract  void                Reset                   ();
        internal  abstract  void                DetectSetPatchesInternal(DetectAllPatches task, ObjectMapper mapper);
        internal  abstract  void                SyncPeerEntityMap       (Dictionary<JsonKey, EntityValue> entityMap, ObjectMapper mapper);
        
        internal  abstract  void                ResetSync               ();
        internal  abstract  SyncTask            SubscribeChangesInternal(Change change);
        internal  abstract  SubscribeChanges    GetSubscription();
        internal  abstract  string              GetKeyName();
        internal  abstract  bool                IsIntKey();
        protected abstract  void                GetRawEntities(List<object> result);
        
        public static       void                GetRawEntities(EntitySet entitySet, List<object> result) => entitySet.GetRawEntities(result);

        protected EntitySet(string name) {
            this.name = name;
        }
    }
    
    // --------------------------------------- EntitySetBase<T> ---------------------------------------
    public abstract class EntitySetBase<T> : EntitySet where T : class
    {
        internal  abstract  SyncSetBase<T>  GetSyncSetBase  ();
        internal  abstract  Peer<T>         GetPeerById     (in JsonKey id);
        internal  abstract  Peer<T>         CreatePeer      (T entity);
        internal  abstract  JsonKey         GetEntityId     (T entity);
        
        protected EntitySetBase(string name) : base(name) { }
        
        internal static void ValidateKeyType(Type keyType) {
            var entityId        = EntityKey.GetEntityKey<T>();
            var entityKeyType   = entityId.GetKeyType();
            // TAG_NULL_REF
            var underlyingKeyType   = Nullable.GetUnderlyingType(keyType);
            if (underlyingKeyType != null) {
                keyType = underlyingKeyType;
            }
            if (keyType == entityKeyType)
                return;
            var type            = typeof(T);
            var entityKeyName   = entityId.GetKeyName();
            var name            = type.Name;
            var keyName         = keyType.Name;
            var error = $"key Type mismatch. {entityKeyType.Name} ({name}.{entityKeyName}) != {keyName} (EntitySet<{keyName},{name}>)";
            throw new InvalidTypeException(error);
        }
    }
}

namespace Friflo.Json.Fliox.Hub.Client
{
    // ---------------------------------- EntitySet<TKey, T> internals ----------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial class EntitySet<TKey, T>
    {
        private SetInfo GetSetInfo() {
            var info = new SetInfo (name) { peers = peerMap?.Count ?? 0 };
            syncSet?.SetTaskInfo(ref info);
            return info;
        }
        
        internal override void Init(FlioxClient store) {
            intern      = new SetIntern<TKey, T>(store, this);
        }
        
        internal override void Reset() {
            peerMap?.Clear();
            intern.writePretty  = ClientStatic.DefaultWritePretty;
            intern.writeNull    = ClientStatic.DefaultWriteNull;
            syncSet             = null;
        }

        // --- internal generic entity utility methods - there public counterparts are at EntityUtils<TKey,T>
        private  static     void    SetEntityId (T entity, in JsonKey id)   => Static.EntityKeyTMap.SetId(entity, id);
        internal override   JsonKey GetEntityId (T entity)                  => Static.EntityKeyTMap.GetId(entity);
        internal static     TKey    GetEntityKey(T entity)                  => Static.EntityKeyTMap.GetKey(entity);

        internal override void DetectSetPatchesInternal(DetectAllPatches allPatches, ObjectMapper mapper) {
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask<TKey,T>(set);
            var peers   = PeerMap();
            foreach (var peerPair in peers) {
                TKey    key     = peerPair.Key;
                Peer<T> peer    = peerPair.Value;
                set.DetectPeerPatches(key, peer, task, mapper);
            }
            if (task.Patches.Count > 0) {
                allPatches.entitySetPatches.Add(task);
                set.AddDetectPatches(task);
                intern.store.AddTask(task);
            }
        }
        
        internal override Peer<T> CreatePeer (T entity) {
            var key   = GetEntityKey(entity);
            var peers = PeerMap();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                peer.SetEntity(entity);
                return peer;
            }
            var id  = Static.KeyConvert.KeyToId(key);
            peer    = new Peer<T>(entity, id);
            peers.Add(key, peer);
            return peer;
        }
        
        internal void DeletePeer (in JsonKey id) {
            var key = Static.KeyConvert.IdToKey(id);
            var peers = PeerMap();
            peers.Remove(key);
        }
        
        [Conditional("DEBUG")]
        private static void AssertId(TKey key, in JsonKey id) {
            var expect = Static.KeyConvert.KeyToId(key);
            if (!id.IsEqual(expect))
                throw new InvalidOperationException($"assigned invalid id: {id}, expect: {expect}");
        }
        
        internal bool TryGetPeerByKey(TKey key, out Peer<T> value) {
            var peers = PeerMap();
            return peers.TryGetValue(key, out value);
        }
        
        internal Peer<T> GetOrCreatePeerByKey(TKey key, JsonKey id) {
            var peers = PeerMap();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            if (id.IsNull()) {
                id = Static.KeyConvert.KeyToId(key);
            } else {
                AssertId(key, id);
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }

        /// use <see cref="GetOrCreatePeerByKey"/> is possible
        internal override Peer<T> GetPeerById(in JsonKey id) {
            var key = Static.KeyConvert.IdToKey(id);
            var peers = PeerMap();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }
        
        // ReSharper disable once UnusedMember.Local
        private bool TryGetPeerByEntity(T entity, out Peer<T> value) {
            var key     = Static.EntityKeyTMap.GetKey(entity); 
            var peers   = PeerMap();
            return peers.TryGetValue(key, out value);
        }
        
        // --- EntitySet
        internal override void SyncPeerEntityMap(Dictionary<JsonKey, EntityValue> entityMap, ObjectMapper mapper) {
            var reader = mapper.reader;

            foreach (var entityPair in entityMap) {
                var id      = entityPair.Key;
                var value   = entityPair.Value;
                var error   = value.Error;
                var peer    = GetPeerById(id);
                if (error != null) {
                    // id & container are not serialized as they are redundant data.
                    // Infer their values from containing dictionary & EntitySet<>
                    error.id        = id;
                    error.container = name;
                    peer.error      = error;
                    continue;
                }
                peer.error  = null;
                var json    = value.Json;
                if (json.IsNull()) {
                    peer.SetPatchSourceNull();
                    continue;    
                }
                var entity  = peer.NullableEntity;
                if (entity == null) {
                    entity  = (T)intern.GetMapper().CreateInstance();
                    SetEntityId(entity, id);
                    peer.SetEntity(entity);
                }
                reader.ReadTo(json, entity);
                if (reader.Success) {
                    peer.SetPatchSource(reader.Read<T>(json));
                } else {
                    var entityError = new EntityError(EntityErrorType.ParseError, name, id, reader.Error.msg.ToString());
                    entityMap[id].SetError(id, entityError);
                }
            }
        }
        
        /// Similar to <see cref="SyncPeerEntityMap"/> but operates on a key and value list.
        internal void SyncPeerEntities(
            List<JsonEntity>        entities,
            ObjectMapper            mapper,
            List<ApplyInfo<TKey,T>> applyInfos)
        {
            var reader  = mapper.reader;
            var count   = entities.Count;
            for (int n = 0; n < count; n++) {
                var jsonEntity  = entities[n];
                var peer        = GetPeerById(jsonEntity.key);

                peer.error  = null;
                var entity  = peer.NullableEntity;
                ApplyInfoType applyType;
                if (entity == null) {
                    applyType   = ApplyInfoType.EntityCreated;
                    entity      = (T)intern.GetMapper().CreateInstance();
                    SetEntityId(entity, jsonEntity.key);
                    peer.SetEntity(entity);
                } else {
                    applyType   = ApplyInfoType.EntityUpdated;
                }
                reader.ReadTo(jsonEntity.value, entity);
                if (reader.Success) {
                    peer.SetPatchSource(reader.Read<T>(jsonEntity.value));
                } else {
                    applyType |= ApplyInfoType.ParseError;
                }
                var key = Static.KeyConvert.IdToKey(jsonEntity.key);
                applyInfos.Add(new ApplyInfo<TKey,T>(applyType, key, entity, jsonEntity.value));
            }
        }
        
        internal void DeletePeerEntities (ICollection<TKey> keys, List<ApplyInfo<TKey,T>> applyInfos) {
            var peers = PeerMap();
            foreach (var key in keys) {
                var found   = peers.Remove(key);
                var type    = found ? ApplyInfoType.EntityDeleted : default;
                applyInfos.Add(new ApplyInfo<TKey,T>(type, key, default, default));
            }
        }
        
        internal void PatchPeerEntities (List<Patch<TKey>> patches, ObjectMapper mapper, List<ApplyInfo<TKey,T>> applyInfos) {
            var reader = mapper.reader;
            foreach (var patch in patches) {
                var applyType   = ApplyInfoType.EntityPatched;
                var peer        = GetOrCreatePeerByKey(patch.key, default);
                var entity      = peer.Entity;
                reader.ReadTo(patch.patch, entity);
                if (reader.Error.ErrSet) {
                    applyType |= ApplyInfoType.ParseError;
                }
                applyInfos.Add(new ApplyInfo<TKey,T>(applyType, patch.key, entity, patch.patch));
            }
        }

        internal override void ResetSync() {
            syncSet    = null;
        }
        
        internal override SyncTask SubscribeChangesInternal(Change change) {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().SubscribeChangesFilter(change, all);
            intern.store.AddTask(task);
            return task;
        }
        
        internal override SubscribeChanges GetSubscription() {
            return intern.subscription;
        }
        
        internal override string GetKeyName() {
            return Static.EntityKeyTMap.GetKeyName();
        }
        
        internal override bool IsIntKey() {
            return Static.EntityKeyTMap.IsIntKey();
        }
        
        protected override  void GetRawEntities(List<object> result) {
            result.Clear();
            foreach (var pair in Local) {
                result.Add(pair.Value);
            }
        }
    }
}
