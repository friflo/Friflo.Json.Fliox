// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// An EntitySet represents a collection (table) of entities (records) of type <typeparamref name="T"/> and their key type <typeparamref name="TKey"/>. <br/>
    /// The methods of an <see cref="EntitySet{TKey,T}"/> enable to create, read, upsert, delete, patch and aggregate container entities.<br/>
    /// It also allows to subscribe to entity changes made by other database clients.
    /// </summary>
    /// <remarks>
    /// <see cref="EntitySet{TKey,T}"/>'s are designed to be used as fields or properties inside a <see cref="FlioxClient"/>. <br/>
    /// The type <typeparamref name="T"/> of a container entity need to be a class containing a field or property used as its <b>key</b> - the primary key. <br/>
    /// This key field is usually named <b>id</b>. Using a different name for the primary key requires the field annotation <b>[Key]</b>.<br/>
    /// Supported <typeparamref name="TKey"/> types are:
    /// <see cref="string"/>, <see cref="long"/>, <see cref="int"/>, <see cref="short"/>, <see cref="byte"/> and <see cref="Guid"/>.
    /// The type of <typeparamref name="TKey"/> must match the <see cref="Type"/> used for the <b>key</b> field / property in an entity class.
    /// In case of a type mismatch a runtime exceptions is thrown.
    /// </remarks>
    /// <typeparam name="TKey">Entity key type</typeparam>
    /// <typeparam name="T">Entity type</typeparam>
    [TypeMapper(typeof(EntitySetMatcher))]
    public sealed partial class EntitySet<TKey, T> : EntitySetBase<T>  where T : class
    {
    #region - Members    
        // Keep all utility related fields of EntitySet in SetIntern (intern) to enhance debugging overview.
        // Reason:  EntitySet<,> is used as field or property by an application which is mainly interested
        //          in following properties while debugging:
        //          Peers, Tasks
                        internal            SetIntern<TKey, T>          intern;         // Use intern struct as first field 
        /// <summary> available in debugger via <see cref="SetIntern{TKey,T}.SyncSet"/> </summary>
        [Browse(Never)] internal            SyncSet<TKey, T>            syncSet;
        /// <summary> key: <see cref="Peer{T}.entity"/>.id </summary>
        [Browse(Never)] private             Dictionary<TKey, Peer<T>>   peerMap;        //  Note: must be private by all means
        
        /// <summary> enable access to entities in debugger. Not used internally. </summary>
        // Note: using Dictionary.Values is okay. The ValueCollection is instantiated only once for a Dictionary instance
        // ReSharper disable once UnusedMember.Local
                        private             IReadOnlyCollection<Peer<T>> Peers => peerMap?.Values;
        
        /// <summary> List of tasks created by its <see cref="EntitySet{TKey,T}"/> methods. These tasks are executed when calling <see cref="FlioxClient.SyncTasks"/> </summary>
        //  Not used internally 
                        public              IReadOnlyList<SyncTask>     Tasks           => syncSet?.tasks;
        /// <summary> Provide access to the <see cref="LocalEntities{TKey,T}"/> tracked by the <see cref="EntitySet{TKey,T}"/> </summary>
        [Browse(Never)] public              LocalEntities<TKey,T>       Local           => local ?? (local = new LocalEntities<TKey, T>(this));
        [Browse(Never)] private             LocalEntities<TKey,T>       local;
        /// Note: must be private by all means
                        private             Dictionary<TKey, Peer<T>>   PeerMap()       => peerMap ?? (peerMap = SyncSet.CreateDictionary<TKey,Peer<T>>());
        /// <summary> Note! Must be called only from <see cref="LocalEntities{TKey,T}"/> to preserve maintainability </summary>
                        internal            Dictionary<TKey, Peer<T>>   GetPeers()      => peerMap;
                        private             SyncSet<TKey, T>            GetSyncSet()    => syncSet ?? (syncSet = syncSetBuffer.Get() ?? new SyncSet<TKey, T>(this));
                        internal override   SyncSetBase<T>              GetSyncSetBase()=> syncSet;
                        public   override   string                      ToString()      => SetInfo.ToString();

        [Browse(Never)] internal override   SyncSet                     SyncSet         => syncSet;
        [Browse(Never)] internal override   SetInfo                     SetInfo         => GetSetInfo();
        [Browse(Never)] internal override   Type                        KeyType         => typeof(TKey);
        [Browse(Never)] internal override   Type                        EntityType      => typeof(T);
        
        /// <summary> If true the serialization of entities to JSON is prettified </summary>
        [Browse(Never)] public   override   bool                        WritePretty { get => intern.writePretty;   set => intern.writePretty = value; }
        /// <summary> If true the serialization of entities to JSON write null fields. Otherwise null fields are omitted </summary>
        [Browse(Never)] public   override   bool                        WriteNull   { get => intern.writeNull;     set => intern.writeNull   = value; }
        
        internal    InstanceBuffer<DeleteTask<TKey,T>>                  deleteBuffer;
        internal    InstanceBuffer<ReadTask<TKey, T>>                   readBuffer;
        internal    InstanceBuffer<SyncSet<TKey,T>>                     syncSetBuffer;

        
        /// <summary> using a static class prevents noise in form of 'Static members' for class instances in Debugger </summary>
        private static class Static  {
            internal static  readonly       EntityKeyT<TKey, T>         EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
            internal static  readonly       KeyConverter<TKey>          KeyConvert      = KeyConverter.GetConverter<TKey>();
        }
        #endregion

        // ----------------------------------------- public methods -----------------------------------------
    #region - initialize     
        /// constructor is called via <see cref="EntitySetMapper{T,TKey,TEntity}.CreateEntitySet"/> 
        internal EntitySet(string name) : base (name) {
            // ValidateKeyType(typeof(TKey)); // only required if constructor is public
        }
        #endregion
        
    #region - Read
        /// <summary>
        /// Create a <see cref="ReadTask{TKey,T}"/> used to read entities <b>by id</b> added with <see cref="ReadTask{TKey,T}.Find"/> subsequently
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public ReadTask<TKey, T> Read() {
            var task = GetSyncSet().Read();
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Query
        /// <summary>
        /// Create a <see cref="QueryTask{TKey, T}"/> with the given LINQ query <paramref name="filter"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public QueryTask<TKey, T> Query(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Query() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, ClientStatic.RefQueryPath);
            var task = GetSyncSet().QueryFilter(op);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="QueryTask{TKey, T}"/> with the given <see cref="EntityFilter{T}"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public QueryTask<TKey, T> QueryByFilter(EntityFilter<T> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.QueryByFilter() filter must not be null. EntitySet: {name}");
            var task = GetSyncSet().QueryFilter(filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="QueryTask{TKey, T}"/> to query all entities of an container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public QueryTask<TKey, T> QueryAll() {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().QueryFilter(all);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Close the <paramref name="cursors"/> returned by <see cref="QueryTask{TKey, T}.ResultCursor"/> of a <see cref="QueryTask{TKey, T}"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CloseCursorsTask CloseCursors(IEnumerable<string> cursors) {
            var task = GetSyncSet().CloseCursors(cursors);
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Aggregate
        /// <summary>
        /// Create a <see cref="CountTask{T}"/> counting all entities matching to the given LINQ query <paramref name="filter"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CountTask<T> Count(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Aggregate() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, ClientStatic.RefQueryPath);
            var task = GetSyncSet().CountFilter(op);
            intern.store.AddTask(task);
            return task;
        }

        /// <summary>
        /// Create a <see cref="CountTask{T}"/> counting all entities matching to the given  <see cref="EntityFilter{T}"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        // ReSharper disable once UnusedMember.Local - may be public in future
        private CountTask<T> CountByFilter(EntityFilter<T> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.AggregateByFilter() filter must not be null. EntitySet: {name}");
            var task = GetSyncSet().CountFilter(filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="CountTask{T}"/> counting all entities in a container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CountTask<T> CountAll() {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().CountFilter(all);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - SubscribeChanges
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <paramref name="change"/>.<br/>
        /// To unsubscribe from receiving change events set <paramref name="change"/> to <see cref="Change.None"/>.<br/>
        /// </summary>
        /// <remarks>
        /// To execute the task call <see cref="FlioxClient.SyncTasks"/> <br/>
        /// The <see cref="Changes{TKey,T}"/> of a subscription event can be applied to an <see cref="EntitySet{TKey,T}"/>
        /// with <see cref="Changes{TKey,T}.ApplyChangesTo"/>.
        /// <br/>
        /// <b>Note:</b> In case using the same <paramref name="filter"/> in multiple queries use <see cref="SubscribeChangesByFilter"/>
        /// to avoid the overhead to convert the <paramref name="filter"/> expression.
        /// <br/>
        /// <b>Note:</b> To ensure remote clients with occasional disconnects get <b>all</b> events set
        /// <see cref="ClientParam.queueEvents"/> in <see cref="StdCommands.Client"/> to true. 
        /// </remarks>
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
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
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <paramref name="change"/>. <br/>
        /// To unsubscribe from receiving change events set <paramref name="change"/> to <see cref="Change.None"/>. <br/>
        /// </summary>
        /// <remarks>
        /// To execute the task call <see cref="FlioxClient.SyncTasks"/> <br/>
        /// The <see cref="Changes{TKey,T}"/> of a subscription event can be applied to an <see cref="EntitySet{TKey,T}"/>
        /// with <see cref="Changes{TKey,T}.ApplyChangesTo"/>. <br/>
        /// <b>Note:</b> In case using the same <paramref name="filter"/> in multiple queries use <see cref="SubscribeChangesByFilter"/>
        /// to avoid the overhead to convert the <paramref name="filter"/> expression.
        /// </remarks> 
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
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
        /// To unsubscribe from receiving change events set <paramref name="change"/> to <see cref="Change.None"/>.
        /// </summary>
        /// <remarks> The <see cref="Changes{TKey,T}"/> of a subscription event can be applied to an <see cref="EntitySet{TKey,T}"/>
        /// with <see cref="Changes{TKey,T}.ApplyChangesTo"/>. <br/>
        /// To execute the task call <see cref="FlioxClient.SyncTasks"/> <br/></remarks>
        /// <remarks><br/>Note: To ensure remote clients with occasional disconnects get <b>all</b> events use <see cref="StdCommands.Client"/></remarks>
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
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
        /// <summary>
        /// Return a <see cref="CreateTask{T}"/> used to create the given <paramref name="entity"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CreateTask<T> Create(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Create() entity must not be null. EntitySet: {name}");
            var task = GetSyncSet().Create(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Return a <see cref="CreateTask{T}"/> used to to create the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CreateTask<T> CreateRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.CreateRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.CreateRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().CreateRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Upsert
        /// <summary>
        /// Create a <see cref="UpsertTask{T}"/> used to upsert the given <paramref name="entity"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public UpsertTask<T> Upsert(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Upsert() entity must not be null. EntitySet: {name}");
            if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                throw new ArgumentException($"EntitySet.Upsert() entity.id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Upsert(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="UpsertTask{T}"/> used to upsert the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public UpsertTask<T> UpsertRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.UpsertRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.UpsertRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().UpsertRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Delete
        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the given <paramref name="entity"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
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

        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the entity with the passed <paramref name="key"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteTask<TKey, T> Delete(TKey key) {
            if (key == null)
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Delete(key);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
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
        
        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the entities with the passed <paramref name="keys"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
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
        
        /// <summary>
        /// Create a <see cref="DeleteAllTask{TKey,T}"/> to delete all entities in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteAllTask<TKey, T> DeleteAll() {
            var task = GetSyncSet().DeleteAll();
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Patch detection
        /// <summary>
        /// Detect <see cref="DetectPatchesTask{TKey,T}.Patches"/> made to all tracked entities.
        /// Detected patches are applied to the container when calling <see cref="FlioxClient.SyncTasks"/>
        /// </summary>
        /// <remarks> Consider using <see cref="DetectPatches(T)"/> or <see cref="DetectPatches(IEnumerable{T})"/>
        /// as this method run detection on all tracked entities. </remarks>
        public DetectPatchesTask<TKey,T> DetectPatches() {
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask<TKey,T>(set);
            var peers   = PeerMap();
            set.AddDetectPatches(task);
            using (var pooled = intern.store.ObjectMapper.Get()) {
                foreach (var peerPair in peers) {
                    TKey    key  = peerPair.Key;
                    Peer<T> peer = peerPair.Value;
                    set.DetectPeerPatches(key, peer, task, pooled.instance);
                }
            }
            intern.store.AddTask(task);
            return task;
        }

        /// <summary>
        /// Detect <see cref="DetectPatchesTask{TKey,T}.Patches"/> made to the passed tracked <paramref name="entity"/>.
        /// Detected patches are applied to the container when calling <see cref="FlioxClient.SyncTasks"/>
        /// </summary>
        public DetectPatchesTask<TKey,T> DetectPatches(T entity) {
            if (entity == null)                             throw new ArgumentNullException(nameof(entity));
            var key     = Static.EntityKeyTMap.GetKey(entity);
            if (Static.KeyConvert.IsKeyNull(key))           throw new ArgumentException($"entity key must not be null.");
            if (!TryGetPeerByKey(key, out var peer))        throw new ArgumentException($"entity is not tracked. key: {key}");
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask<TKey,T>(set);
            set.AddDetectPatches(task);
            using (var pooled = intern.store.ObjectMapper.Get()) {
                set.DetectPeerPatches(key, peer, task, pooled.instance);
            }
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Detect <see cref="DetectPatchesTask{TKey,T}.Patches"/> made to the passed tracked <paramref name="entities"/>.
        /// Detected patches are applied to the container when calling <see cref="FlioxClient.SyncTasks"/>
        /// </summary>
        public DetectPatchesTask<TKey,T> DetectPatches(IEnumerable<T> entities) {
            if(entities == null)                            throw new ArgumentNullException(nameof(entities));
            int n       = 0;
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask<TKey,T>(set);
            set.AddDetectPatches(task);
            using (var pooled = intern.store.ObjectMapper.Get()) {
                foreach (var entity in entities) {
                    if (entity == null)                         throw new ArgumentException($"entities[{n}] is null");
                    var key     = Static.EntityKeyTMap.GetKey(entity);
                    if (Static.KeyConvert.IsKeyNull(key))       throw new ArgumentException($"entity key must not be null. entities[{n}]");
                    if (!TryGetPeerByKey(key, out var peer))    throw new ArgumentException($"entity is not tracked. entities[{n}] key: {key}");
                    set.DetectPeerPatches(key, peer, task, pooled.instance);
                    n++;
                }
            }
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Read relations
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
