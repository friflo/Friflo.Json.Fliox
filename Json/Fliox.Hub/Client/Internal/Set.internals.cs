// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Transform;

// EntitySet & EntitySetBase<T> are not intended as a public API.
// These classes are declared here to simplify navigation to EntitySet<TKey, T>.
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    // --------------------------------------- EntitySetBase<T> ---------------------------------------
    internal abstract partial class Set<T> : Set where T : class
    {
        internal    InstanceBuffer<CreateTask<T>>   createBuffer;
        internal    InstanceBuffer<UpsertTask<T>>   upsertBuffer;

        internal  abstract  JsonKey         TrackEntity      (T entity, PeerState state);
        
        protected Set(in SetInit init) : base(init) { }
        
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

    // ---------------------------------- EntitySet<TKey, T> internals ----------------------------------
    internal partial class Set<TKey, T>
    {
        private TypeMapper<T>  GetTypeMapper() => intern.typeMapper   ??= (TypeMapper<T>)client._readonly.typeStore.GetTypeMapper(typeof(T));

        
        private SetInfo GetSetInfo() {
            var info    = new SetInfo (name) { peers = peerMap.Count };
            var tasks   = GetTasks();
            SetTaskInfo(ref info, tasks);
            return info;
        }
        
        internal SyncTask[] GetTasks() {
            var allTasks    = client._intern.syncStore.tasks.GetReadOnlySpan();
            var count       = 0;
            foreach (var task in allTasks) {
                if (task.taskSet == this) {
                    count++;
                }
            }
            if (count == 0) {
                return Array.Empty<SyncTask>();    
            }
            var tasks = new SyncTask[count];
            var n = 0;
            foreach (var task in allTasks) {
                if (task.taskSet == this) {
                    tasks[n++] = task;
                }
            }
            return tasks;
        }
        
        internal DetectPatchesTask<TKey,T> DetectPatches() {
            var task    = new DetectPatchesTask<TKey,T>(this);
            AddDetectPatches(task);
            var mapper = client.ObjectMapper();
            foreach (var peerPair in peerMap) {
                TKey    key  = peerPair.Key;
                Peer<T> peer = peerPair.Value;
                DetectPeerPatches(key, peer, task, mapper);
            }
            return task;
        }
        
        internal override void Reset() {
            peerMap.Clear();
            intern.writePretty  = ClientStatic.DefaultWritePretty;
            intern.writeNull    = ClientStatic.DefaultWriteNull;
        }

        // --- internal generic entity utility methods - there public counterparts are at EntityUtils<TKey,T>
        private  static     void    SetEntityId (T entity, in JsonKey id)   => EntityKeyTMap.SetId(entity, id);
        internal static     TKey    GetEntityKey(T entity)                  => EntityKeyTMap.GetKey(entity);

        internal override void DetectSetPatchesInternal(DetectAllPatches allPatches, ObjectMapper mapper) {
            var task    = new DetectPatchesTask<TKey,T>(this);
            var peers   = peerMap;
            foreach (var peerPair in peers) {
                TKey    key     = peerPair.Key;
                Peer<T> peer    = peerPair.Value;
                DetectPeerPatches(key, peer, task, mapper);
            }
            if (task.Patches.Count > 0) {
                allPatches.entitySetPatches.Add(task);
                AddDetectPatches(task);
                client.AddTask(task);
            }
        }
        
        internal override JsonKey TrackEntity (T entity, PeerState state) {
            var key   = GetEntityKey(entity);
            var peers = peerMap;
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                peer.SetEntity(entity);
                peer.state = state;
                return peer.id;
            }
            var id  = KeyConvert.KeyToId(key);
            peer    = new Peer<T>(entity, id);
            peer.state = state;
            peers.Add(key, peer);
            return id;
        }
        
        private void DeletePeers () {
            peerMap.Clear();
        }
        
        private void DeletePeer (in JsonKey id) {
            var key = KeyConvert.IdToKey(id);
            peerMap.Remove(key);
        }
        
        [Conditional("DEBUG")]
        private static void AssertId(TKey key, in JsonKey id) {
            var expect = KeyConvert.KeyToId(key);
            if (!id.IsEqual(expect))
                throw new InvalidOperationException($"assigned invalid id: {id}, expect: {expect}");
        }
        
        internal bool TryGetPeer(TKey key, out Peer<T> value) {
            return peerMap.TryGetValue(key, out value);
        }
        
        private T GetOrCreateEntity(TKey key, JsonKey id, out  Peer<T> peer) {
            if (peerMap.TryGetValue(key, out peer)) {
                return peer.NullableEntity;
            }
            if (id.IsNull()) {
                id = KeyConvert.KeyToId(key);
            } else {
                AssertId(key, id);
            }
            peer = new Peer<T>(id);
            peerMap.Add(key, peer);
            return peer.NullableEntity;
        }

        private Peer<T> GetPeerById(in JsonKey id) {
            var key = KeyConvert.IdToKey(id);
            var peers = peerMap;
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }
        
        // --- EntitySet
        internal void SyncPeerEntities(
            List<JsonValue>         values,
            List<JsonKey>           keys,
            ObjectMapper            mapper,
            List<ApplyInfo<TKey,T>> applyInfos)
        {
            if (values.Count != keys.Count) throw new InvalidOperationException("expect values.Count == keys.Count");
            var reader      = mapper.reader;
            var count       = values.Count;
            var typeMapper  = GetTypeMapper();
            for (int n = 0; n < count; n++) {
                var id          = keys[n];
                var peer        = GetPeerById(id);

                peer.SetError(null);
                var entity  = peer.NullableEntity;
                ApplyInfoType applyType;
                if (entity == null) {
                    applyType   = ApplyInfoType.EntityCreated;
                    entity      = (T)typeMapper.NewInstance();
                    SetEntityId(entity, id);
                    peer.SetEntity(entity);
                } else {
                    applyType   = ApplyInfoType.EntityUpdated;
                }
                var value = values[n];
                reader.ReadToMapper(typeMapper, value, entity, false);
                if (reader.Success) {
                    peer.SetPatchSource(value);
                } else {
                    applyType |= ApplyInfoType.ParseError;
                }
                var key = KeyConvert.IdToKey(id);
                applyInfos.Add(new ApplyInfo<TKey,T>(applyType, key, entity, value));
            }
        }
        
        private Entity ReadEntity (in EntityValue value, ObjectReader reader) {
            var id      = KeyConvert.IdToKey(value.key);
            var entity  = GetOrCreateEntity(id, value.key, out var peer);
            var error = value.Error;
            if (error != null) {
                peer.SetError(value.Error);
                return new Entity(null, error);
            }
            var json = value.Json;
            if (json.IsNull()) {
                peer.SetEntity(null);   // Could delete peer instead
                peer.SetPatchSourceNull();
                return new Entity(null, null);
            }
            var typeMapper  = GetTypeMapper();
            if (entity == null) {
                entity          = (T)typeMapper.NewInstance();
                SetEntityId(entity, peer.id);
                peer.SetEntity(entity);
            }
            reader.ReadToMapper(typeMapper, json, entity, false);
            if (reader.Success) {
                peer.SetPatchSource(json);
                return new Entity(entity, null);
            }
            var entityError = new EntityError(EntityErrorType.ParseError, nameShort, peer.id, reader.Error.msg.ToString());
            return new Entity(null, entityError);
        }
        
        // ---------------------------- get EntityValue[] from results ----------------------------
        /// <summary>Counterpart of <see cref="RemoteHostUtils.ResponseToJson"/></summary>
        private EntityValue[] GetReadResultValues (ReadEntitiesResult result) {
            if (!client._readonly.isRemoteHub) {
                return result.entities.Values;
            }
            return JsonToEntities(result.set, result.notFound, result.errors);
        }
        
        private EntityValue[] GetQueryResultValues (QueryEntitiesResult result) {
            if (!client._readonly.isRemoteHub) {
                return result.entities.Values;
            }
            return JsonToEntities(result.set, null, result.errors);
        }
        
        private EntityValue[] GetReferencesResultValues (ReferencesResult result) {
            if (!client._readonly.isRemoteHub) {
                return result.entities.Values;
            }
            return JsonToEntities(result.set, null, result.errors);
        }
        
        internal override Entity[] AddReferencedEntities (ReferencesResult referenceResult, ObjectReader reader)
        {
            var values  = GetReferencesResultValues(referenceResult);
            var entities = new Entity[values.Length];
            
            for (int n = 0; n < values.Length; n++) {
                var value   = values[n];
                entities[n] = ReadEntity(value, reader);
            }
            return entities;
        }
        
        // ------------------------------------------------------------------------------------------
        internal void DeletePeerEntities (List<Delete<TKey>> deletes, List<ApplyInfo<TKey,T>> applyInfos) {
            var peers = peerMap;
            foreach (var delete in deletes) {
                var found   = peers.Remove(delete.key);
                var type    = found ? ApplyInfoType.EntityDeleted : default;
                applyInfos.Add(new ApplyInfo<TKey,T>(type, delete.key, default, default));
            }
        }
        
        /// Called on patch events
        internal void PatchPeerEntities (List<Patch<TKey>> patches, ObjectMapper mapper, List<ApplyInfo<TKey,T>> applyInfos) {
            var reader      = mapper.reader;
            var typeMapper  = GetTypeMapper();
            foreach (var patch in patches) {
                var applyType   = ApplyInfoType.EntityPatched;
                // patch only existing peers
                if (!TryGetPeer(patch.key, out var peer)) {
                    continue;
                }
                // patch only if peer entity is not null
                var entity = peer.NullableEntity;
                if (entity == null) {
                    continue;
                }
                reader.ReadToMapper(typeMapper, patch.patch, entity, false);
                if (reader.Error.ErrSet) {
                    applyType |= ApplyInfoType.ParseError;
                }
                applyInfos.Add(new ApplyInfo<TKey,T>(applyType, patch.key, entity, patch.patch));
            }
        }

        internal override SyncTask SubscribeChangesInternal(Change change) {
            var all = Operation.FilterTrue;
            var task = SubscribeChangesFilter(change, all);
            client.AddTask(task);
            return task;
        }
        
        internal override SubscribeChanges GetSubscription() {
            return intern.subscription;
        }
        
        internal override  void GetRawEntities(List<object> result) {
            foreach (var pair in Local) {
                result.Add(pair.Value);
            }
        }
    }
}
