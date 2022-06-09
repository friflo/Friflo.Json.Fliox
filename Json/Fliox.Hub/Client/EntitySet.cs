// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;


// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// An EntitySet represents a collection (table) of entities (records) with a specific type <typeparamref name="T"/>. <br/>
    /// <br/>
    /// The methods of an <see cref="EntitySet{TKey,T}"/> enable to create, read, upsert or delete container entities. <br/>
    /// It also allows to subscribe to entity changes made by other database clients. <br/>
    /// <br/>
    /// <see cref="EntitySet{TKey,T}"/>'s are designed to be used as fields or properties inside a <see cref="FlioxClient"/>. <br/>
    /// The type <typeparamref name="T"/> of a container entity need to be a class containing a field or property used as its key
    /// usually named <b>id</b>. <br/>
    /// Supported <typeparamref name="TKey"/> types are:
    /// <see cref="string"/>, <see cref="long"/>, <see cref="int"/>, <see cref="short"/>, <see cref="byte"/>
    /// and <see cref="Guid"/>.
    /// <br/>
    /// The key type <typeparamref name="TKey"/> must match the <see cref="Type"/> used for the key field / property in an entity class.
    /// In case of a type mismatch a runtime exceptions is thrown.
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [TypeMapper(typeof(EntitySetMatcher))]
    public sealed partial class EntitySet<TKey, T> : EntitySetBase<T>  where T : class
    {
    #region - Members    
        // Keep all utility related fields of EntitySet in SetIntern (intern) to enhance debugging overview.
        // Reason:  EntitySet<,> is used as field or property by an application which is mainly interested
        //          in following fields or properties while debugging:
        //          name, _peers & SetInfo
        internal            SetIntern<TKey, T>          intern;
        
        /// key: <see cref="Peer{T}.entity"/>.id        Note: must be private by all means
        private             Dictionary<TKey, Peer<T>>   _peers;
        /// create peers map on demand.                 Note: must be private by all means
        private             Dictionary<TKey, Peer<T>>   Peers() => _peers ?? (_peers = SyncSet.CreateDictionary<TKey,Peer<T>>());
        
        internal static readonly EntityKeyT<TKey, T>    EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private  static readonly KeyConverter<TKey>     KeyConvert      = KeyConverter.GetConverter<TKey>();

        [DebuggerBrowsable(Never)]
        private             SyncSet<TKey, T>            syncSet;
        private             SyncSet<TKey, T>            GetSyncSet()    => syncSet ?? (syncSet = new SyncSet<TKey, T>(this));
        internal override   SyncSetBase<T>              GetSyncSetBase()=> syncSet;

        internal override   SyncSet                     SyncSet         => syncSet;
        public   override   string                      ToString()      => SetInfo.ToString();

        [DebuggerBrowsable(Never)] internal override    Type    KeyType      => typeof(TKey);
        [DebuggerBrowsable(Never)] internal override    Type    EntityType   => typeof(T);
        
        [DebuggerBrowsable(Never)] public   override    bool    WritePretty { get => intern.writePretty;   set => intern.writePretty = value; }
        [DebuggerBrowsable(Never)] public   override    bool    WriteNull   { get => intern.writeNull;     set => intern.writeNull   = value; }

        internal override   SetInfo                     SetInfo { get {
            var info = new SetInfo (name) { peers = _peers?.Count ?? 0 };
            syncSet?.SetTaskInfo(ref info);
            return info;
        }}
        #endregion
        
        /// constructor is called via <see cref="EntitySetMapper{T,TKey,TEntity}.CreateEntitySet"/> 
        internal EntitySet(string name) : base (name) {
            // ValidateKeyType(typeof(TKey)); // only required if constructor is public
        }
        
        // --------------------------------------- public interface ---------------------------------------
    #region - Cache    
        public bool TryGet (TKey key, out T entity) {
            var peers = Peers();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                entity = peer.NullableEntity;
                return true;
            }
            entity = null;
            return false;
        }
        
        public List<T> AsList() {
            var peers   = Peers();
            var result  = new List<T>(peers.Count);
            foreach (var pair in peers) {
                var entity = pair.Value.NullableEntity;
                if (entity == null)
                    continue;
                result.Add(entity);
            }
            return result;
        }
        
        public bool Contains (TKey key) {
            var peers = Peers();
            return peers.ContainsKey(key);
        }
        #endregion
        
    #region - Read
        public ReadTask<TKey, T> Read() {
            // ReadTasks<> are not added with intern.store.AddTask(task) as it only groups the tasks created via its
            // methods like: Find(), FindRange(), ReadRefTask() & ReadRefsTask().
            // A ReadTask<> its self cannot fail.
            return GetSyncSet().Read();
        }
        #endregion

    #region - Query
        public QueryTask<T> Query(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Query() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, RefQueryPath);
            var task = GetSyncSet().QueryFilter(op);
            intern.store.AddTask(task);
            return task;
        }
        
        public QueryTask<T> QueryByFilter(EntityFilter<T> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.QueryByFilter() filter must not be null. EntitySet: {name}");
            var task = GetSyncSet().QueryFilter(filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        public QueryTask<T> QueryAll() {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().QueryFilter(all);
            intern.store.AddTask(task);
            return task;
        }
        
        public CloseCursorsTask CloseCursors(IEnumerable<string> cursors) {
            var task = GetSyncSet().CloseCursors(cursors);
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Aggregate
        public CountTask<T> Count(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Aggregate() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, RefQueryPath);
            var task = GetSyncSet().CountFilter(op);
            intern.store.AddTask(task);
            return task;
        }

        // ReSharper disable once UnusedMember.Local - may be public in future 
        private CountTask<T> CountByFilter(EntityFilter<T> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.AggregateByFilter() filter must not be null. EntitySet: {name}");
            var task = GetSyncSet().CountFilter(filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        public CountTask<T> CountAll() {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().CountFilter(all);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - SubscribeChanges
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <paramref name="change"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to null.
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChangesFilter(Change change, Expression<Func<T, bool>> filter, ChangeSubscriptionHandler<TKey, T> handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (filter == null)  throw new ArgumentNullException(nameof(filter));
            intern.store.AssertSubscription();
            var op = Operation.FromFilter(filter);
            var task = GetSyncSet().SubscribeChangesFilter(change, op);
            intern.store.AddTask(task);
            changeCallback = new GenericChangeCallback<TKey,T>(handler);
            return task;
        }
        
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the <paramref name="change"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to null.
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChangesByFilter(Change change, EntityFilter<T> filter, ChangeSubscriptionHandler<TKey, T> handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (filter == null)  throw new ArgumentNullException(nameof(filter));
            intern.store.AssertSubscription();
            var task = GetSyncSet().SubscribeChangesFilter(change, filter.op);
            intern.store.AddTask(task);
            changeCallback = new GenericChangeCallback<TKey,T>(handler);
            return task;
        }
        
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <paramref name="change"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to null.
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChanges(Change change, ChangeSubscriptionHandler<TKey, T> handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            intern.store.AssertSubscription();
            var all = Operation.FilterTrue;
            var task = GetSyncSet().SubscribeChangesFilter(change, all);
            intern.store.AddTask(task);
            changeCallback = new GenericChangeCallback<TKey,T>(handler);
            return task;
        }
        #endregion
        
    #region - ReserveKeys
        public ReserveKeysTask<TKey, T> ReserveKeys(int count) {
            var task = GetSyncSet().ReserveKeys(count);
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Create
        public CreateTask<T> Create(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Create() entity must not be null. EntitySet: {name}");
            var task = GetSyncSet().Create(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        public CreateTask<T> CreateRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.CreateRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.CreateRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().CreateRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Upsert
        public UpsertTask<T> Upsert(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Upsert() entity must not be null. EntitySet: {name}");
            if (EntityKeyTMap.IsEntityKeyNull(entity))
                throw new ArgumentException($"EntitySet.Upsert() entity.id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Upsert(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        public UpsertTask<T> UpsertRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.UpsertRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.UpsertRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().UpsertRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Delete
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
        
        public DeleteAllTask<TKey, T> DeleteAll() {
            var task = GetSyncSet().DeleteAll();
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Patch
        // - assign patches
        public PatchTask<T> Patch(MemberSelectionBuilder<T> selection) {
            var memberSelection = new MemberSelection<T>();
            selection(memberSelection);
            var task = GetSyncSet().Patch(memberSelection);
            intern.store.AddTask(task);
            return task;
        }
        
        public PatchTask<T> Patch(MemberSelection<T> memberSelection) {
            var task = GetSyncSet().Patch(memberSelection);
            intern.store.AddTask(task);
            return task;
        }
        
        // - detect patches
        public DetectPatchesTask DetectPatches() {
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask(set);
            var peers   = Peers();
            using (var pooled = intern.store.ObjectMapper.Get()) {
                set.DetectSetPatches(peers, task, pooled.instance);
            }
            intern.store.AddTask(task);
            return task;
        }

        public DetectPatchesTask DetectEntityPatches(T entity) {
            if (entity == null)                         throw new ArgumentNullException(nameof(entity));
            if (EntityKeyTMap.IsEntityKeyNull(entity))  throw new ArgumentException($"entity key must not be null.");
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask(set);
            set.DetectEntityPatches(entity, task);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Relation
        public RelationPath<TRef> RelationPath<TRefKey, TRef>(
            EntitySet<TRefKey, TRef>        relation,
            Expression<Func<T, TRefKey>>    selector) where TRef : class
        {
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationPath<TRef>(path);
        }
        
        public RelationsPath<TRef> RelationsPath<TRefKey, TRef>(
            EntitySet<TRefKey, TRef>                    relation,
            Expression<Func<T, IEnumerable<TRefKey>>>   selector) where TRef : class
        {
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationPath<TRef>(path);
        }
        #endregion
    }
}
