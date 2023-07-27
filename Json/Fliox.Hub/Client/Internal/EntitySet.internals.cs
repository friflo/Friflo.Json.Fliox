// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// EntitySet & EntitySetBase<T> are not intended as a public API.
// These classes are declared here to simplify navigation to EntitySet<TKey, T>.
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    // --------------------------------------- EntitySet ---------------------------------------
    internal abstract class EntitySet
    {
        [DebuggerBrowsable(Never)] internal readonly  string          name;
        [DebuggerBrowsable(Never)] internal readonly  int             index;
        [DebuggerBrowsable(Never)] internal readonly  ShortString     nameShort;
        [DebuggerBrowsable(Never)] internal           ChangeCallback  changeCallback;
        

        internal  abstract  SyncSet     SyncSet     { get; }
        internal  abstract  SetInfo     SetInfo     { get; }
        internal  abstract  Type        KeyType     { get; }
        internal  abstract  Type        EntityType  { get; }
        internal  abstract  bool        WritePretty { get; set; }
        internal  abstract  bool        WriteNull   { get; set; }
        
        internal  abstract  void                Reset                   ();
        internal  abstract  void                DetectSetPatchesInternal(DetectAllPatches task, ObjectMapper mapper);
        internal  abstract  void                SyncPeerEntityMap       (Dictionary<JsonKey, EntityValue> entityMap, ObjectMapper mapper);
        internal  abstract  void                SyncPeerObjectMap       (Dictionary<JsonKey, object>      objectMap);
        
        internal  abstract  void                ResetSync               ();
        internal  abstract  SyncTask            SubscribeChangesInternal(Change change);
        internal  abstract  SubscribeChanges    GetSubscription();
        internal  abstract  string              GetKeyName();
        internal  abstract  bool                IsIntKey();
        internal  abstract  void                GetRawEntities(List<object> result);
        
        protected EntitySet(string name, int index) {
            this.name   = name;
            this.index  = index;
            nameShort   = new ShortString(name);
        }
        
        internal static void SetTaskInfo(ref SetInfo info, SyncTask[] tasks) {
            foreach (var syncTask in tasks) {
                switch (syncTask.TaskType) {
                    case TaskType.read:             info.read++;                break;
                    case TaskType.query:            info.query++;               break;
                    case TaskType.aggregate:        info.aggregate++;           break;
                    case TaskType.create:           info.create++;              break;
                    case TaskType.upsert:           info.upsert++;              break;
                    case TaskType.merge:            info.merge++;               break;
                    case TaskType.delete:           info.delete++;              break;
                    case TaskType.closeCursors:     info.closeCursors++;        break;
                    case TaskType.subscribeChanges: info.subscribeChanges++;    break;
                    case TaskType.reserveKeys:      info.reserveKeys++;         break;
                }
            }
            info.tasks =
                info.read               +
                info.query              +
                info.aggregate          +
                info.closeCursors       +
                info.create             +
                info.upsert             +
                info.merge              +
                info.delete             +
                info.subscribeChanges   +
                info.reserveKeys;
        }
    }
    
    // --------------------------------------- EntitySetBase<T> ---------------------------------------
    internal abstract class EntitySetBase<T> : EntitySet where T : class
    {
        internal  InstanceBuffer<CreateTask<T>>     createBuffer;
        internal  InstanceBuffer<UpsertTask<T>>     upsertBuffer;
        internal  InstanceBuffer<UpsertEntities>    upsertEntitiesBuffer;
        
        internal  abstract  SyncSetBase<T>  GetSyncSetBase  ();
        internal  abstract  Peer<T>         GetPeerById     (in JsonKey id);
        internal  abstract  Peer<T>         CreatePeer      (T entity);
        internal  abstract  JsonKey         GetEntityId     (T entity);
        
        protected EntitySetBase(string name, int index) : base(name, index) { }
        
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
    internal partial class EntitySetInstance<TKey, T>
    {
        private TypeMapper<T>  GetTypeMapper() => intern.typeMapper   ??= (TypeMapper<T>)client._readonly.typeStore.GetTypeMapper(typeof(T));

        
        private SetInfo GetSetInfo() {
            var info    = new SetInfo (name) { peers = peerMap?.Count ?? 0 };
            var tasks   = GetTasks();
            SetTaskInfo(ref info, tasks);
            return info;
        }
        
        internal SyncTask[] GetTasks() {
            var allTasks    = client._intern.syncStore.tasks.GetReadOnlySpan();
            var count       = 0;
            foreach (var task in allTasks) {
                if (task.entitySetName == name) {
                    count++;
                }
            }
            if (count == 0) {
                return Array.Empty<SyncTask>();    
            }
            var tasks = new SyncTask[count];
            var n = 0;
            foreach (var task in allTasks) {
                if (task.entitySetName == name) {
                    tasks[n++] = task;
                }
            }
            return tasks;
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
                client.AddTask(task);
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
            var reader      = mapper.reader;
            var typeMapper  = GetTypeMapper();
            foreach (var entityPair in entityMap) {
                var id      = entityPair.Key;
                var value   = entityPair.Value;
                var error   = value.Error;
                var peer    = GetPeerById(id);
                if (error != null) {
                    // id & container are not serialized as they are redundant data.
                    // Infer their values from containing dictionary & EntitySet<>
                    error.id        = id;
                    error.container = nameShort;
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
                    entity  = (T)typeMapper.NewInstance();
                    SetEntityId(entity, id);
                    peer.SetEntity(entity);
                }
                reader.ReadToMapper(typeMapper, json, entity, false);
                if (reader.Success) {
                    peer.SetPatchSource(json);
                } else {
                    var entityError = new EntityError(EntityErrorType.ParseError, nameShort, id, reader.Error.msg.ToString());
                    // entityMap[id].SetError(id, entityError); - used when using class EntityValue
                    // [c# - Editing dictionary values in a foreach loop - Stack Overflow] https://stackoverflow.com/questions/1070766/editing-dictionary-values-in-a-foreach-loop
                    entityMap[id] = new EntityValue(id, entityError);
                }
            }
        }
        
        internal override void SyncPeerObjectMap (Dictionary<JsonKey, object> objectMap) {
            var typeMapper  = GetTypeMapper();
            foreach (var entityPair in objectMap) {
                var id      = entityPair.Key;
                var obj     = (T)entityPair.Value;
                var peer    = GetPeerById(id);
                /* var error   = value.Error;
                if (error != null) {
                    // id & container are not serialized as they are redundant data.
                    // Infer their values from containing dictionary & EntitySet<>
                    error.id        = id;
                    error.container = nameShort;
                    peer.error      = error;
                    continue;
                } */
                peer.error  = null;
                var current = peer.NullableEntity;
                if (current != null) {
                    if (obj == null) {
                        peer.SetPatchSourceNull();
                        peer.SetEntity(null);
                    } else {
                        typeMapper.MemberwiseCopy(obj, current);
                        // TODO set patch source
                    }
                } else {
                    CreatePeer(obj);
                    // TODO set patch source
                }
            }
        }
        
        /// Similar to <see cref="SyncPeerEntityMap"/> but operates on a key and value list.
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

                peer.error  = null;
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
                var key = Static.KeyConvert.IdToKey(id);
                applyInfos.Add(new ApplyInfo<TKey,T>(applyType, key, entity, value));
            }
        }
        
        internal void DeletePeerEntities (List<Delete<TKey>> deletes, List<ApplyInfo<TKey,T>> applyInfos) {
            var peers = PeerMap();
            foreach (var delete in deletes) {
                var found   = peers.Remove(delete.key);
                var type    = found ? ApplyInfoType.EntityDeleted : default;
                applyInfos.Add(new ApplyInfo<TKey,T>(type, delete.key, default, default));
            }
        }
        
        internal void PatchPeerEntities (List<Patch<TKey>> patches, ObjectMapper mapper, List<ApplyInfo<TKey,T>> applyInfos) {
            var reader      = mapper.reader;
            var typeMapper  = GetTypeMapper();
            foreach (var patch in patches) {
                var applyType   = ApplyInfoType.EntityPatched;
                var peer        = GetOrCreatePeerByKey(patch.key, default);
                var entity      = peer.Entity;
                reader.ReadToMapper(typeMapper, patch.patch, entity, false);
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
            client.AddTask(task);
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
        
        internal override  void GetRawEntities(List<object> result) {
            foreach (var pair in Local) {
                result.Add(pair.Value);
            }
        }
    }
}
