// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Database;
using Friflo.Json.Fliox.Graph.Internal;
using Friflo.Json.Fliox.Graph.Internal.Id;
using Friflo.Json.Fliox.Graph.Internal.Map;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Sync;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query;

// EntitySet & EntitySetBase<T> are not intended as a public API.
// These classes are declared here to simplify navigation to EntitySet<TKey, T>.
namespace Friflo.Json.Fliox.Graph.Internal
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  readonly  string      name;

        internal  abstract  SyncSet     SyncSet     { get; }
        internal  abstract  SetInfo     SetInfo     { get; }

        internal static readonly QueryPath RefQueryPath = new RefQueryPath();
        
        internal  abstract  void                Init                    (EntityStore store);
        internal  abstract  void                LogSetChangesInternal   (LogTask logTask);
        internal  abstract  void                SyncPeerEntities        (Dictionary<JsonKey, EntityValue> entities);
        internal  abstract  void                DeletePeerEntities      (HashSet   <JsonKey> ids);
        internal  abstract  void                PatchPeerEntities       (Dictionary<JsonKey, EntityPatch> patches);
        
        internal  abstract  void                ResetSync               ();
        internal  abstract  SyncTask            SubscribeChangesInternal(IEnumerable<Change> changes);
        internal  abstract  SubscribeChanges    GetSubscription();

        protected EntitySet(string name) {
            this.name = name;
        }
    }
    
    public abstract class EntitySetBase<T> : EntitySet where T : class
    {
        internal  abstract  SyncSetBase<T>  GetSyncSetBase();
        
        internal  abstract  Peer<T>         GetPeerById (in JsonKey id);
        internal  abstract  Peer<T>         GetPeerByEntity(T entity);
        
        internal  abstract  Peer<T>         CreatePeer (T entity);
        internal  abstract  JsonKey         GetEntityId (T entity);

        protected EntitySetBase(string name) : base(name) { }
        
        internal static bool ValidateKeyType(Type keyType, out string error) {
            var entityId        = EntityId.GetEntityId<T>();
            var entityKeyType   = entityId.GetKeyType();
            var entityKeyName   = entityId.GetKeyName();
            if (keyType != entityKeyType) {
                var type = typeof(T);
                error = $"key Type mismatch. {entityKeyType.Name} ({type.Name}.{entityKeyName}) != {keyType.Name} (EntitySet<{keyType.Name},{type.Name}>)";
                return false;
            }
            error = null;
            return true;
        }
    }
}

namespace Friflo.Json.Fliox.Graph
{

    /// <summary>
    /// An EntitySet represents a collection (table) of entities (records).
    /// <br/>
    /// The methods of an <see cref="EntitySet{TKey,T}"/> enable to create, read, update or delete container entities.
    /// It also allows to subscribe to entities changes made by other database users.<br/>
    /// <see cref="EntitySet{TKey,T}"/>'s are designed to be used as fields or properties inside an <see cref="EntityStore"/>.
    /// <br/>
    /// The type <see cref="T"/> of a container entity need to be a class containing a field or property used as its key
    /// usually named <b>id</b>.
    /// Supported <see cref="TKey"/> types are:
    /// <see cref="string"/>, <see cref="long"/>, <see cref="int"/>, <see cref="short"/>, <see cref="byte"/>
    /// and <see cref="Guid"/><br/>.
    /// The key type <see cref="TKey"/> must match the <see cref="Type"/> used for the key field / property.
    /// In case of a type mismatch a runtime exceptions is thrown.
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [Fri.TypeMapper(typeof(EntitySetMatcher))]
    public class EntitySet<TKey, T> : EntitySetBase<T>  where T : class
    {
        // Keep all utility related fields of EntitySet in SetIntern to enhance debugging overview.
        // Reason:  EntitySet is extended by application which is mainly interested in following fields while debugging:
        //          peers, Sync, name, container & store 
        internal            SetIntern<TKey, T>          intern;
        
        /// key: <see cref="Peer{T}.entity"/>.id        Note: must be private by all means
        private  readonly   Dictionary<TKey, Peer<T>>   peers = new Dictionary<TKey, Peer<T>>();
        
        private static readonly   EntityId<T>  EntityKey = EntityId.GetEntityKey<TKey, T>();

        
        // ReSharper disable once NotAccessedField.Local
        private             EntityContainer             container; // not used - only for debugging ergonomics
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private             SyncSet<TKey, T>            syncSet;
        internal            SyncSet<TKey, T>            GetSyncSet()    => syncSet ?? (syncSet = new SyncSet<TKey, T>(this));
        internal override   SyncSetBase<T>              GetSyncSetBase()=> syncSet;

        internal override   SyncSet                     SyncSet         => syncSet;
        public   override   string                      ToString()      => SetInfo.ToString();
        
        internal override   SetInfo                     SetInfo { get {
            var info = new SetInfo (name) { peers = peers.Count };
            syncSet?.SetTaskInfo(ref info);
            return info;
        }}
        
        internal override void Init(EntityStore store) {
            var type = typeof(T);
            store._intern.setByType[type]       = this;
            store._intern.setByName[type.Name]  = this;
            container   = store._intern.database.GetOrCreateContainer(name);
            intern      = new SetIntern<TKey, T>(store);
        }
        
        // --------------------------------------- public interface --------------------------------------- 
        public EntitySet() : base (typeof(T).Name) {
            if (!ValidateKeyType(typeof(TKey), out string error)) {
                throw new InvalidTypeException(error);
            }
        }

        // --- Read
        public ReadTask<TKey, T> Read() {
            // ReadTasks<> are not added with intern.store.AddTask(task) as it only groups the tasks created via its
            // methods like: Find(), FindRange(), ReadRefTask() & ReadRefsTask().
            // A ReadTask<> its self cannot fail.
            return GetSyncSet().Read();
        }

        // --- Query
        public QueryTask<TKey, T> Query(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Query() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, RefQueryPath);
            var task = GetSyncSet().QueryFilter(op);
            intern.store.AddTask(task);
            return task;
        }
        
        public QueryTask<TKey, T> QueryByFilter(EntityFilter<T> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.QueryByFilter() filter must not be null. EntitySet: {name}");
            var task = GetSyncSet().QueryFilter(filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        public QueryTask<TKey, T> QueryAll() {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().QueryFilter(all);
            intern.store.AddTask(task);
            return task;
        }
        
        // --- SubscribeChanges
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <see cref="changes"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To react on specific changes use <see cref="EntityStore.SetSubscriptionHandler"/>.
        /// To unsubscribe from receiving change events set <see cref="changes"/> to null.
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChangesFilter(IEnumerable<Change> changes, Expression<Func<T, bool>> filter) {
            intern.store.AssertSubscriptionProcessor();
            var op = Operation.FromFilter(filter);
            var task = GetSyncSet().SubscribeChangesFilter(changes, op);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the <see cref="changes"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To react on specific changes use <see cref="EntityStore.SetSubscriptionHandler"/>.
        /// To unsubscribe from receiving change events set <see cref="changes"/> to null.
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChangesByFilter(IEnumerable<Change> changes, EntityFilter<T> filter) {
            intern.store.AssertSubscriptionProcessor();
            var task = GetSyncSet().SubscribeChangesFilter(changes, filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <see cref="changes"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To react on specific changes use <see cref="EntityStore.SetSubscriptionHandler"/>.
        /// To unsubscribe from receiving change events set <see cref="changes"/> to null.
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChanges(IEnumerable<Change> changes) {
            intern.store.AssertSubscriptionProcessor();
            var all = Operation.FilterTrue;
            var task = GetSyncSet().SubscribeChangesFilter(changes, all);
            intern.store.AddTask(task);
            return task;
        }

        // --- Create
        public CreateTask<T> Create(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Create() entity must not be null. EntitySet: {name}");
            if (EntityKey.IsEntityKeyNull(entity))
                throw new ArgumentException($"EntitySet.Create() entity.id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Create(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        public CreateTask<T> CreateRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.CreateRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (EntityKey.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.CreateRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().CreateRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        
        // --- Update
        public UpdateTask<T> Update(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Update() entity must not be null. EntitySet: {name}");
            if (EntityKey.IsEntityKeyNull(entity))
                throw new ArgumentException($"EntitySet.Update() entity.id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Update(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        public UpdateTask<T> UpdateRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.UpdateRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (EntityKey.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.UpdateRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().UpdateRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        
        // --- Patch
        public PatchTask<T> Patch(T entity) {
            var peer = GetPeerByEntity(entity);
            peer.SetEntity(entity);
            var task = GetSyncSet().Patch(peer);
            intern.store.AddTask(task);
            return task;
        }
        
        public PatchTask<T> PatchRange(ICollection<T> entities) {
            var peerList = new List<Peer<T>>(entities.Count);
            foreach (var entity in entities) {
                var peer = GetPeerByEntity(entity);
                peer.SetEntity(entity);
                peerList.Add(peer);
            }
            var task = GetSyncSet().PatchRange(peerList);
            intern.store.AddTask(task);
            return task;
        }

        // --- Delete
        public DeleteTask<TKey, T> Delete(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Delete() entity must not be null. EntitySet: {name}");
            var key = GetEntityKey(entity);
            if (key == null)
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Delete(key);
            intern.store.AddTask(task);
            return task;
        }

        public DeleteTask<TKey, T> Delete(TKey key) {
            if (key == null)
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Delete(key);
            intern.store.AddTask(task);
            return task;
        }
        
        public DeleteTask<TKey, T> DeleteRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.DeleteRange() entities must not be null. EntitySet: {name}");
            var keys = new List<TKey>(entities.Count);
            foreach (var entity in entities) {
                var key = GetEntityKey(entity);
                keys.Add(key);
            }
            foreach (var key in keys) {
                if (key == null) throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().DeleteRange(keys);
            intern.store.AddTask(task);
            return task;
        }
        
        public DeleteTask<TKey, T> DeleteRange(ICollection<TKey> keys) {
            if (keys == null)
                throw new ArgumentException($"EntitySet.DeleteRange() ids must not be null. EntitySet: {name}");
            foreach (var key in keys) {
                if (key == null) throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().DeleteRange(keys);
            intern.store.AddTask(task);
            return task;
        }

        // --- Log changes -> create patches
        public LogTask LogSetChanges() {
            var task = intern.store._intern.syncStore.CreateLog();
            GetSyncSet().LogSetChanges(peers, task);
            intern.store.AddTask(task);
            return task;
        }

        public LogTask LogEntityChanges(T entity) {
            var task = intern.store._intern.syncStore.CreateLog();
            if (entity == null)
                throw new ArgumentException($"EntitySet.LogEntityChanges() entity must not be null. EntitySet: {name}");
            if (EntityKey.IsEntityKeyNull(entity))
                throw new ArgumentException($"EntitySet.LogEntityChanges() entity.id must not be null. EntitySet: {name}");
            GetSyncSet().LogEntityChanges(entity, task);
            intern.store.AddTask(task);
            return task;
        }
        
        // --- create RefPath / RefsPath
        public RefPath<T, TKey, TRef> RefPath<TRef>(Expression<Func<T, Ref<TKey, TRef>>> selector) where TRef : class {
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefPath<T, TKey, TRef>(path);
        }
        
        public RefsPath<T, TKey, TRef> RefsPath<TRef>(Expression<Func<T, IEnumerable<Ref<TKey, TRef>>>> selector) where TRef : class {
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefsPath<T, TKey, TRef>(path);
        }
        
        // ------------------------------------------- internals -------------------------------------------
        private static void SetEntityId (T entity, in JsonKey id) {
            Ref<TKey, T>.EntityKey.SetId(entity, id);
        }
        
        internal override JsonKey GetEntityId (T entity) {
            return Ref<TKey, T>.EntityKey.GetId(entity);
        }
        
        private static void SetEntityKey (T entity, TKey key) {
            Ref<TKey, T>.EntityKey.SetKey(entity, key);
        }
        
        private static TKey GetEntityKey (T entity) {
            return Ref<TKey, T>.EntityKey.GetKey(entity);
        }

        internal override void LogSetChangesInternal(LogTask logTask) {
            GetSyncSet().LogSetChanges(peers, logTask);
        }
        
        internal override Peer<T> CreatePeer (T entity) {
            var key = GetEntityKey(entity);
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                peer.SetEntity(entity);
                return peer;
            }
            var id = GetEntityId(entity);
            peer = new Peer<T>(entity, id);
            peers.Add(key, peer);
            return peer;
        }
        
        internal void DeletePeer (in JsonKey id) {
            var key = Ref<TKey,T>.EntityKey.IdToKey(id);
            peers.Remove(key);
        }
        
        [Conditional("DEBUG")]
        private static void AssertId(TKey key, in JsonKey id) {
            var expect = Ref<TKey,T>.EntityKey.KeyToId(key);
            if (!id.IsEqual(expect))
                throw new InvalidOperationException($"assigned invalid id: {id}, expect: {expect}");
        }
        
        internal  bool GetPeerByRef(Ref<TKey, T> reference, out Peer<T> peer) {
            if (reference.IsNull()) {
                peer = null;
                return false; // todo add test
            }
            var set = reference.GetSet();
            if (set == null) {
                var entity = reference.GetEntity();
                if (entity != null) {
                    peer = CreatePeer(entity);
                    return true;
                }
                peer = GetOrCreatePeerByKey(reference.key, new JsonKey());
                return true;
            }
            peer = set.GetPeerByKey(reference.key);
            return true;
        }
        
        internal Peer<T> GetPeerByKey(TKey key) {
            return peers[key];
        }
        
        internal Peer<T> GetOrCreatePeerByKey(TKey key, JsonKey id) {
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            if (id.IsNull()) {
                id = Ref<TKey,T>.EntityKey.KeyToId(key);
            } else {
                AssertId(key, id);
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }

        /// use <see cref="GetOrCreatePeerByKey"/> is possible
        internal override Peer<T> GetPeerById(in JsonKey id) {
            var key = Ref<TKey,T>.EntityKey.IdToKey(id);
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }
        
        internal override Peer<T> GetPeerByEntity(T entity) {
            var key = GetEntityKey(entity);
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            var id = Ref<TKey,T>.EntityKey.KeyToId(key);
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }
        
        // --- EntitySet
        internal override void SyncPeerEntities(Dictionary<JsonKey, EntityValue> entities) {
            var reader = intern.jsonMapper.reader;
                
            foreach (var entityPair in entities) {
                var id = entityPair.Key;
                var value = entityPair.Value;
                var error = value.Error;
                var peer = GetPeerById(id);
                if (error != null) {
                    // id & container are not serialized as they are redundant data.
                    // Infer their values from containing dictionary & EntitySet<>
                    error.id        = id;
                    error.container = name;
                    peer.error      = error;
                    continue;
                }

                peer.error = null;
                var json = value.Json;
                if (json != null && "null" != json) {
                    var entity = peer.NullableEntity;
                    if (entity == null) {
                        entity = (T)intern.typeMapper.CreateInstance();
                        SetEntityId(entity, id);
                        peer.SetEntity(entity);
                    }
                    reader.ReadTo(json, entity);
                    if (reader.Success) {
                        peer.SetPatchSource(reader.Read<T>(json));
                    } else {
                        var entityError = new EntityError(EntityErrorType.ParseError, name, id, reader.Error.msg.ToString());
                        entities[id].SetError(entityError);
                    }
                } else {
                    peer.SetPatchSourceNull();
                }
                peer.assigned = true;
            }
        }
        
        internal  override void DeletePeerEntities (HashSet<JsonKey> ids) {
            foreach (var id in ids) {
                DeletePeer(id);
            }
        }
        
        internal  override void PatchPeerEntities (Dictionary<JsonKey, EntityPatch> patches) {
            var objectPatcher = intern.store._intern.objectPatcher;
            foreach (var pair in patches) {
                var         id          = pair.Key;
                EntityPatch entityPatch = pair.Value;
                var         peer        = GetPeerById(id);
                var         entity      = peer.Entity;
                objectPatcher.ApplyPatches(entity, entityPatch.patches);
            }
        }

        internal override void ResetSync() {
            syncSet    = null;
        }
        
        internal override SyncTask SubscribeChangesInternal(IEnumerable<Change> changes) {
            return SubscribeChanges(changes);    
        }
        
        internal override SubscribeChanges GetSubscription() {
            return intern.subscription;
        }
    }
}