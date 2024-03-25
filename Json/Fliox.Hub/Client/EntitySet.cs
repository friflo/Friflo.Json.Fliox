// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Client
{
    // ------------------------------------------ EntitySet<TKey,T> ------------------------------------------
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
    /// The type of <typeparamref name="TKey"/> must match the <see cref="Type"/> used for the <b>[Key]</b> field / property in an entity class.
    /// In case of a type mismatch a runtime exceptions is thrown.
    /// </remarks>
    /// <typeparam name="TKey">Entity key type</typeparam>
    /// <typeparam name="T">Entity type</typeparam>
    [TypeMapper(typeof(EntitySetMatcher))]
    public readonly struct EntitySet<TKey, T> where T : class
    {
    #region - public properties
        /// <summary> If true the serialization of entities to JSON is prettified </summary>
        [Browse(Never)] public  bool                    WritePretty { get => GetInstance().intern.writePretty;   set => GetInstance().intern.writePretty = value; }

        /// <summary> If true the serialization of entities to JSON write null fields. Otherwise null fields are omitted </summary>
        [Browse(Never)] public  bool                    WriteNull   { get => GetInstance().intern.writeNull;     set => GetInstance().intern.writeNull   = value; }
        
        /// <summary>
        /// Utility methods for type safe key conversion and generic <typeparamref name="TKey"/> access for entities of type <typeparamref name="T"/>
        /// </summary>
        [Browse(Never)] public  SetUtils<TKey,T>        Utils       => Static.SetUtils;
        
        /// <summary> List of tasks created by its <see cref="EntitySet{TKey,T}"/> methods. These tasks are executed when calling <see cref="FlioxClient.SyncTasks"/> </summary>
        //  Not used internally 
                        public  SyncTask[]              Tasks       => GetInstance().GetTasks();
        
        /// <summary> Provide access to the <see cref="LocalEntities{TKey,T}"/> tracked by the <see cref="EntitySet{TKey,T}"/> </summary>
                        public  LocalEntities<TKey,T>   Local       => GetLocal();
        
                        public  string                  Name        => client._readonly.entityInfos[index].container;
                        
        [Browse(Never)] public  ShortString             NameShort   => client._readonly.entityInfos[index].containerShort;

                        public  override string         ToString()  => GetString();
        #endregion
                        
    #region - internal fields
                        private readonly FlioxClient    client;

        [Browse(Never)] private readonly int            index;

                        private Set<TKey, T>            Instance    => (Set<TKey, T>)client.entitySets[index];
        
                        
        /// <summary> using a static class prevents noise in form of 'Static members' for class instances in Debugger </summary>
        private static class Static  {
            internal static  readonly   EntityKeyT<TKey, T>         EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
            internal static  readonly   KeyConverter<TKey>          KeyConvert      = KeyConverter.GetConverter<TKey>();
            internal static  readonly   SetUtils<TKey, T>           SetUtils        = new SetUtils<TKey, T>();
        }
        #endregion
        
    #region - Initialize
        /// constructor is called via <see cref="GenericContainerMember{TKey,T}.SetContainerMember"/> 
        internal EntitySet(FlioxClient client, int index)  {
            this.client = client;
            this.index  = index;
        }
        
        /// <summary>
        /// Create an <see cref="EntitySet{TKey,T}"/> for the given client.<br/>
        /// The <see cref="EntitySetInfo"/> can be retrieved from <see cref="FlioxClient.GetEntitySetInfos"/>
        /// </summary>
        public EntitySet(FlioxClient client, EntitySetInfo info)  {
            if (typeof(TKey) != info.keyType)    throw new ArgumentException($"expect TKey: {typeof(TKey).Name}. was: {info.keyType.Name}");
            if (typeof(T)    != info.entityType) throw new ArgumentException($"expect T: {typeof(T).Name}. was: {info.entityType.Name}");
            this.client = client;
            this.index  = info.index;
        }
        #endregion
        
    #region - Read
        /// <summary>
        /// Create a <see cref="ReadTask{TKey,T}"/> used to read entities <b>by id</b> added with <see cref="ReadTask{TKey,T}.Find"/> subsequently
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public ReadTask<TKey, T> Read() {
            var instance = GetInstance();
            var task = instance.Read();
            client.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="FindTask{TKey,T}"/> used to read a single entity.
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public FindTask<TKey, T> Find(TKey key) {
            var instance = GetInstance();
            var task = instance.Find(key);
            client.AddTask(task);
            return task;
        }
        #endregion

    #region - Query
        /// <summary>
        /// Create a <see cref="QueryTask{TKey, T}"/> with the given LINQ query <paramref name="filter"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public QueryTask<TKey, T> Query(Expression<Func<T, bool>> filter) {
            var instance = GetInstance();
            if (filter == null)
                throw new ArgumentException($"EntitySet.Query() filter must not be null. EntitySet: {instance.name}");
            var op = Operation.FromFilter(filter, ClientStatic.RefQueryPath);
            var task = instance.QueryFilter(op);
            client.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="QueryTask{TKey, T}"/> with the given <see cref="EntityFilter{T}"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public QueryTask<TKey, T> QueryByFilter(EntityFilter<T> filter) {
            var instance = GetInstance();
            if (filter == null)
                throw new ArgumentException($"EntitySet.QueryByFilter() filter must not be null. EntitySet: {instance.name}");
            var task = instance.QueryFilter(filter.op);
            client.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="QueryTask{TKey, T}"/> to query all entities of an container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public QueryTask<TKey, T> QueryAll() {
            var instance = GetInstance();
            var all = Operation.FilterTrue;
            var task = instance.QueryFilter(all);
            client.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Close the <paramref name="cursors"/> returned by <see cref="QueryTask{TKey, T}.ResultCursor"/> of a <see cref="QueryTask{TKey, T}"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CloseCursorsTask CloseCursors(IEnumerable<string> cursors) {
            var instance = GetInstance();
            var task = instance.CloseCursors(cursors);
            client.AddTask(task);
            return task;
        }
        #endregion

    #region - Aggregate
        /// <summary>
        /// Create a <see cref="CountTask{T}"/> counting all entities matching to the given LINQ query <paramref name="filter"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CountTask<T> Count(Expression<Func<T, bool>> filter) {
            var instance = GetInstance();
            if (filter == null)
                throw new ArgumentException($"EntitySet.Aggregate() filter must not be null. EntitySet: {instance.name}");
            var op = Operation.FromFilter(filter, ClientStatic.RefQueryPath);
            var task = instance.CountFilter(op);
            client.AddTask(task);
            return task;
        }

        /// <summary>
        /// Create a <see cref="CountTask{T}"/> counting all entities matching to the given  <see cref="EntityFilter{T}"/>
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        // ReSharper disable once UnusedMember.Local - may be public in future
        private CountTask<T> CountByFilter(EntityFilter<T> filter) {
            var instance = GetInstance();
            if (filter == null)
                throw new ArgumentException($"EntitySet.AggregateByFilter() filter must not be null. EntitySet: {instance.name}");
            var task = instance.CountFilter(filter.op);
            client.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Create a <see cref="CountTask{T}"/> counting all entities in a container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CountTask<T> CountAll() {
            var instance = GetInstance();
            var all = Operation.FilterTrue;
            var task = instance.CountFilter(all);
            client.AddTask(task);
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
            var instance = GetInstance();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (filter == null)  throw new ArgumentNullException(nameof(filter));
            client.AssertSubscription();
            var op = Operation.FromFilter(filter);
            var task = instance.SubscribeChangesFilter(change, op);
            client.AddTask(task);
            instance.changeCallback = new GenericChangeCallback<TKey,T>(handler);
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
            var instance = GetInstance();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (filter == null)  throw new ArgumentNullException(nameof(filter));
            client.AssertSubscription();
            var task = instance.SubscribeChangesFilter(change, filter.op);
            client.AddTask(task);
            instance.changeCallback = new GenericChangeCallback<TKey,T>(handler);
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
            var instance = GetInstance();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            client.AssertSubscription();
            var all = Operation.FilterTrue;
            var task = instance.SubscribeChangesFilter(change, all);
            client.AddTask(task);
            instance.changeCallback = new GenericChangeCallback<TKey,T>(handler);
            return task;
        }
        #endregion
        
    #region - ReserveKeys
        public ReserveKeysTask<TKey, T> ReserveKeys(int count) {
            var instance = GetInstance();
            var task = instance.ReserveKeys(count);
            client.AddTask(task);
            return task;
        }
        #endregion

    #region - Create
        /// <summary>
        /// Return a <see cref="CreateTask{T}"/> used to create the given <paramref name="entity"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CreateTask<T> Create(T entity) {
            var instance = GetInstance();
            if (entity == null)
                throw new ArgumentException($"EntitySet.Create() entity must not be null. EntitySet: {instance.name}");
            var create  = instance.CreateCreateTask();
            create.Add(entity);
            client.AddTask(create);
            return create;
        }
        
        /// <summary>
        /// Return a <see cref="CreateTask{T}"/> used to to create the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CreateTask<T> CreateRange(List<T> entities) {
            var instance = GetInstance();

            if (entities == null)
                throw new ArgumentException($"EntitySet.CreateRange() entities must not be null. EntitySet: {instance.name}");
            foreach (var entity in entities) {
                if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.CreateRange() entity.id must not be null. EntitySet: {instance.name}");
            }
            var create  = instance.CreateCreateTask();
            create.AddRange(entities);
            client.AddTask(create);
            return create;
        }
        
        /// <summary>
        /// Return a <see cref="CreateTask{T}"/> used to to create the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public CreateTask<T> CreateRange(ICollection<T> entities) {
            var instance = GetInstance();

            if (entities == null)
                throw new ArgumentException($"EntitySet.CreateRange() entities must not be null. EntitySet: {instance.name}");
            foreach (var entity in entities) {
                if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.CreateRange() entity.id must not be null. EntitySet: {instance.name}");
            }
            var create  = instance.CreateCreateTask();
            create.AddRange(entities);
            client.AddTask(create);
            return create;
        }
        #endregion
        
    #region - Upsert
        /// <summary>
        /// Create a <see cref="UpsertTask{T}"/> used to upsert the given <paramref name="entity"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public UpsertTask<T> Upsert(T entity) {
            var instance = GetInstance();

            if (entity == null)
                throw new ArgumentException($"EntitySet.Upsert() entity must not be null. EntitySet: {instance.name}");
            if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                throw new ArgumentException($"EntitySet.Upsert() entity.id must not be null. EntitySet: {instance.name}");
            var upsert  = instance.CreateUpsertTask();
            upsert.Add(entity);
            client.AddTask(upsert);
            return upsert;
        }
        
        /// <summary>
        /// Create a <see cref="UpsertTask{T}"/> used to upsert the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public UpsertTask<T> UpsertRange(List<T> entities) {
            var instance = GetInstance();
            if (entities == null)
                throw new ArgumentException($"EntitySet.UpsertRange() entities must not be null. EntitySet: {instance.name}");
            foreach (var entity in entities) {
                if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.UpsertRange() entity.id must not be null. EntitySet: {instance.name}");
            }
            var upsert  = instance.CreateUpsertTask();
            upsert.AddRange(entities);
            client.AddTask(upsert);
            return upsert;
        }
        
        /// <summary>
        /// Create a <see cref="UpsertTask{T}"/> used to upsert the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public UpsertTask<T> UpsertRange(ICollection<T> entities) {
            var instance = GetInstance();

            if (entities == null)
                throw new ArgumentException($"EntitySet.UpsertRange() entities must not be null. EntitySet: {instance.name}");
            foreach (var entity in entities) {
                if (Static.EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.UpsertRange() entity.id must not be null. EntitySet: {instance.name}");
            }
            var upsert  = instance.CreateUpsertTask();
            upsert.AddRange(entities);
            client.AddTask(upsert);
            return upsert;
        }
        #endregion
        
    #region - Delete
        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the given <paramref name="entity"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteTask<TKey, T> Delete(T entity) {
            var instance = GetInstance();

            if (entity == null)
                throw new ArgumentException($"EntitySet.Delete() entity must not be null. EntitySet: {instance.name}");
            var key = Set<TKey,T>.GetEntityKey(entity);
            if (key == null) {
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {instance.name}");
            }
            var delete  = instance.CreateDeleteTask();
            delete.Add(key);
            client.AddTask(delete);
            return delete;
        }

        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the entity with the passed <paramref name="key"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteTask<TKey, T> Delete(TKey key) {
            var instance = GetInstance();

            if (key == null)
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {instance.name}");
            var delete  = instance.CreateDeleteTask();
            delete.Add(key);
            client.AddTask(delete);
            return delete;
        }
        
        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the given <paramref name="entities"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteTask<TKey, T> DeleteRange(ICollection<T> entities) {
            var instance = GetInstance();

            if (entities == null)
                throw new ArgumentException($"EntitySet.DeleteRange() entities must not be null. EntitySet: {instance.name}");
            var keys = new List<TKey>(entities.Count);
            foreach (var entity in entities) {
                var key = Set<TKey,T>.GetEntityKey(entity);
                keys.Add(key);
            }
            foreach (var key in keys) {
                if (key == null) throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {instance.name}");
            }
            var delete  = instance.CreateDeleteTask();
            delete.AddRange(keys);
            client.AddTask(delete);
            return delete;
        }
        
        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the entities with the passed <paramref name="keys"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteTask<TKey, T> DeleteRange(List<TKey> keys) {
            var instance = GetInstance();

            if (keys == null)
                throw new ArgumentException($"EntitySet.DeleteRange() ids must not be null. EntitySet: {instance.name}");
            foreach (var key in keys) {
                if (key == null) throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {instance.name}");
            }
            var delete  = instance.CreateDeleteTask();
            delete.AddRange(keys);
            client.AddTask(delete);
            return delete;
        }
        
        /// <summary>
        /// Create a <see cref="DeleteTask{TKey,T}"/> to delete the entities with the passed <paramref name="keys"/> in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteTask<TKey, T> DeleteRange(ICollection<TKey> keys) {
            var instance = GetInstance();
            if (keys == null)
                throw new ArgumentException($"EntitySet.DeleteRange() ids must not be null. EntitySet: {instance.name}");
            foreach (var key in keys) {
                if (key == null) throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {instance.name}");
            }
            var delete  = instance.CreateDeleteTask();
            delete.AddRange(keys);
            client.AddTask(delete);
            return delete;
        }
        
        /// <summary>
        /// Create a <see cref="DeleteAllTask{TKey,T}"/> to delete all entities in the container
        /// </summary>
        /// <remarks> To execute the task call <see cref="FlioxClient.SyncTasks"/> </remarks>
        public DeleteAllTask<TKey, T> DeleteAll() {
            var instance = GetInstance();
            var task = instance.DeleteAll();
            client.AddTask(task);
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
            FlioxClient.AssertTrackEntities(client, nameof(DetectPatches));
            var instance    = GetInstance();
            var task        = instance.DetectPatches();
            client.AddTask(task);
            return task;
        }

        /// <summary>
        /// Detect <see cref="DetectPatchesTask{TKey,T}.Patches"/> made to the passed tracked <paramref name="entity"/>.
        /// Detected patches are applied to the container when calling <see cref="FlioxClient.SyncTasks"/>
        /// </summary>
        public DetectPatchesTask<TKey,T> DetectPatches(T entity) {
            FlioxClient.AssertTrackEntities(client, nameof(DetectPatches));
            var instance = GetInstance();
            if (entity == null)                             throw new ArgumentNullException(nameof(entity));
            var key     = Static.EntityKeyTMap.GetKey(entity);
            if (Static.KeyConvert.IsKeyNull(key))           throw new ArgumentException($"entity key must not be null.");
            if (!instance.TryGetPeer(key, out var peer))    throw new ArgumentException($"entity is not tracked. key: {key}");
            var task    = new DetectPatchesTask<TKey,T>(instance);
            instance.AddDetectPatches(task);
            var objectMapper = client.ObjectMapper();
            instance.DetectPeerPatches(key, peer, task, objectMapper);
            client.AddTask(task);
            return task;
        }
        
        /// <summary>
        /// Detect <see cref="DetectPatchesTask{TKey,T}.Patches"/> made to the passed tracked <paramref name="entities"/>.
        /// Detected patches are applied to the container when calling <see cref="FlioxClient.SyncTasks"/>
        /// </summary>
        public DetectPatchesTask<TKey,T> DetectPatches(IEnumerable<T> entities) {
            FlioxClient.AssertTrackEntities(client, nameof(DetectPatches));
            var instance = GetInstance();
            if(entities == null)                            throw new ArgumentNullException(nameof(entities));
            int n       = 0;
            var task    = new DetectPatchesTask<TKey,T>(instance);
            instance.AddDetectPatches(task);
            var mapper = client.ObjectMapper();
            foreach (var entity in entities) {
                if (entity == null)                         throw new ArgumentException($"entities[{n}] is null");
                var key     = Static.EntityKeyTMap.GetKey(entity);
                if (Static.KeyConvert.IsKeyNull(key))       throw new ArgumentException($"entity key must not be null. entities[{n}]");
                if (!instance.TryGetPeer(key, out var peer))throw new ArgumentException($"entity is not tracked. entities[{n}] key: {key}");
                instance.DetectPeerPatches(key, peer, task, mapper);
                n++;
            }
            client.AddTask(task);
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
        
    #region - Internal methods
        private string GetString() {
            var instance = Instance;
            if (instance == null) {
                var container = client._readonly.entityInfos[index].container;
                return new SetInfo(container).ToString();
            }
            return instance.SetInfo.ToString();
        }

        internal Set<TKey, T> GetInstance() {
            var instance = client.entitySets[index];
            if (instance != null) {
                return (Set<TKey,T>)instance;
            }
            return (Set<TKey,T>)client.CreateEntitySet(index);
        }
        
        private LocalEntities<TKey,T> GetLocal() {
            FlioxClient.AssertTrackEntities(client, nameof(Local));
            return GetInstance().Local;
        } 
        #endregion
    }
}
