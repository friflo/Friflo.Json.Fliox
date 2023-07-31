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
        [DebuggerBrowsable(Never)] internal readonly    FlioxClient     client;
        [DebuggerBrowsable(Never)] internal readonly    string          name;
        [DebuggerBrowsable(Never)] internal readonly    int             index;
        [DebuggerBrowsable(Never)] internal readonly    ShortString     nameShort;
        [DebuggerBrowsable(Never)] internal             ChangeCallback  changeCallback;
        

        internal  abstract  SyncSet     SyncSet     { get; }
        internal  abstract  SetInfo     SetInfo     { get; }
        internal  abstract  Type        KeyType     { get; }
        internal  abstract  Type        EntityType  { get; }
        internal  abstract  bool        WritePretty { get; set; }
        internal  abstract  bool        WriteNull   { get; set; }
        
        internal  abstract  void                Reset                   ();
        internal  abstract  void                DetectSetPatchesInternal(DetectAllPatches task, ObjectMapper mapper);
        internal  abstract  void                ResetSync               ();
        internal  abstract  SyncTask            SubscribeChangesInternal(Change change);
        internal  abstract  SubscribeChanges    GetSubscription();
        internal  abstract  string              GetKeyName();
        internal  abstract  bool                IsIntKey();
        internal  abstract  void                GetRawEntities(List<object> result);
        internal  abstract  EntityValue[]       GetReferencesValues (ReferencesResult referenceResult, ObjectReader reader);
        
        protected EntitySet(string name, int index, FlioxClient client) {
            this.name   = name;
            this.index  = index;
            this.client = client;
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
        
        /// <summary>Counterpart of <see cref="EntitiesToJson"/></summary>
        //  SYNC_READ : JSON -> entities
        internal EntityValue[] JsonToEntities(
            ListOne<JsonValue>  set,
            List<JsonKey>       notFound,
            List<EntityError>   errors)
        {
            var processor   = client._intern.EntityProcessor();
            var keyName     = GetKeyName();
            var values = new EntityValue[set.Count + (notFound?.Count ?? 0) + (errors?.Count ?? 0)];
            var n = 0;
            foreach (var value in set.GetReadOnlySpan()) {
                if (processor.GetEntityKey(value, keyName, out var key, out var error)) {
                    values[n++] = new EntityValue(key, value);
                } else {
                    throw new InvalidOperationException($"missing key int result: {error}");
                }
            }
            if (notFound != null) {
                foreach (var key in notFound) {
                    values[n++] = new EntityValue(key);
                }
            }
            if (errors != null) {
                foreach (var error in errors) {
                    error.container = nameShort; // container name is not serialized as it is redundant data.
                    values[n++]     = new EntityValue(error.id, error);
                }
            }
            return values;
        }
        
        /// <summary>Counterpart of <see cref="JsonToEntities"/></summary>
        //  SYNC_READ : entities -> JSON
        internal static void EntitiesToJson(
            EntityValue[]           values,
            out ListOne<JsonValue>  set,
            out List<JsonKey>       notFound,
            out List<EntityError>   errors)
        {
            set         = new ListOne<JsonValue>(values.Length);
            errors      = null;
            notFound    = null;
            foreach (var value in values) {
                var error = value.Error;
                if (error != null) {
                    errors ??= new List<EntityError>();
                    errors.Add(error);
                    continue;
                }
                if (!value.Json.IsNull()) {
                    set.Add(value.Json);
                } else {
                    notFound ??= new List<JsonKey>();
                    notFound.Add(value.key);
                }
            }
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
        
        protected EntitySetBase(string name, int index, FlioxClient client) : base(name, index, client) { }
        
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
        internal TypeMapper<T>  GetTypeMapper() => intern.typeMapper   ??= (TypeMapper<T>)client._readonly.typeStore.GetTypeMapper(typeof(T));

        
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
        
        internal DetectPatchesTask<TKey,T> DetectPatches() {
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask<TKey,T>(set);
            set.AddDetectPatches(task);
            using (var pooled = client.ObjectMapper.Get()) {
                foreach (var peerPair in peerMap) {
                    TKey    key  = peerPair.Key;
                    Peer<T> peer = peerPair.Value;
                    set.DetectPeerPatches(key, peer, task, pooled.instance);
                }
            }
            return task;
        }
        
        internal override void Reset() {
            peerMap.Clear();
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
            var peers   = peerMap;
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
            var peers = peerMap;
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
            peerMap.Remove(key);
        }
        
        [Conditional("DEBUG")]
        private static void AssertId(TKey key, in JsonKey id) {
            var expect = Static.KeyConvert.KeyToId(key);
            if (!id.IsEqual(expect))
                throw new InvalidOperationException($"assigned invalid id: {id}, expect: {expect}");
        }
        
        internal bool TryGetPeerByKey(TKey key, out Peer<T> value) {
            return peerMap.TryGetValue(key, out value);
        }
        
        internal Peer<T> GetOrCreatePeerByKey(TKey key, JsonKey id) {
            if (peerMap.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            if (id.IsNull()) {
                id = Static.KeyConvert.KeyToId(key);
            } else {
                AssertId(key, id);
            }
            peer = new Peer<T>(id);
            peerMap.Add(key, peer);
            return peer;
        }

        /// use <see cref="GetOrCreatePeerByKey"/> if possible
        internal override Peer<T> GetPeerById(in JsonKey id) {
            var key = Static.KeyConvert.IdToKey(id);
            var peers = peerMap;
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
            return peerMap.TryGetValue(key, out value);
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
        
        internal T AddEntity (in EntityValue value, Peer<T> peer, ObjectReader reader, out EntityError entityError) {
            var error = value.Error;
            if (error != null) {
                entityError     = error;
                return null;
            }
            var json = value.Json;
            if (json.IsNull()) {
                peer.SetEntity(null);   // Could delete peer instead
                peer.SetPatchSourceNull();
                entityError = null;
                return null;
            }
            var typeMapper  = GetTypeMapper();
            var entity      = peer.NullableEntity;
            if (entity == null) {
                entity          = (T)typeMapper.NewInstance();
                SetEntityId(entity, peer.id);
                peer.SetEntity(entity);
            }
            reader.ReadToMapper(typeMapper, json, entity, false);
            if (reader.Success) {
                peer.SetPatchSource(json);
                entityError = null;
                return entity;
            }
            entityError = new EntityError(EntityErrorType.ParseError, nameShort, peer.id, reader.Error.msg.ToString());
            return null;
        }
        
        internal override EntityValue[] GetReferencesValues (ReferencesResult referenceResult, ObjectReader reader) {
            EntityValue[]   values; 
            if (!client._readonly.hub.IsRemoteHub) {
                values = referenceResult.entities.values;
            } else {
                values  = JsonToEntities(referenceResult.set, null, referenceResult.errors);
                var ids = new ListOne<JsonKey>(values.Length);
                foreach (var value in values) {
                    ids.Add(value.key);
                }
                referenceResult.ids = ids;
            }
            for (int n = 0; n < values.Length; n++) {
                var value   = values[n];
                var id      = Static.KeyConvert.IdToKey(value.key);
                var peer    = GetOrCreatePeerByKey(id, value.key);
                AddEntity(value, peer, reader, out var error);
                if (error != null) {
                    peer.error = error;
                }
            }
            return values;
        }
        
        internal void DeletePeerEntities (List<Delete<TKey>> deletes, List<ApplyInfo<TKey,T>> applyInfos) {
            var peers = peerMap;
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
