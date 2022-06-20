// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal abstract class SyncSetBase <T> : SyncSet where T : class
    {
        internal abstract void AddUpsert (Peer<T> peer);
        internal abstract void AddCreate (Peer<T> peer);
        internal abstract void AddEntityPatches(PatchTask<T> patchTask, ICollection<T> entities);
    }

    /// Multiple instances of this class can be created when calling <see cref="FlioxClient.SyncTasks"/> without
    /// awaiting the result. Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal sealed partial class SyncSet<TKey, T> : SyncSetBase<T> where T : class
    {
        private static readonly EntityKeyT<TKey, T> EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private static readonly KeyConverter<TKey>  KeyConvert      = KeyConverter.GetConverter<TKey>();

        // --- private members
        // Note!
        // All fields & getters must be private by all means to ensure that all scheduled tasks of a SyncTasks() call
        // managed by this instance can be mapped to their task results safely.

        private     readonly EntitySet<TKey, T>     set;

        // --- backing fields for lazy-initialized getters
        private     List<ReadTask<TKey, T>>         _readTasks;
        private     int                             readTasksIndex;

        private     List<QueryTask<T>>              _queryTasks;
        private     int                             queriesTasksIndex;

        private     List<AggregateTask>             _aggregateTasks;
        private     int                             aggregatesTasksIndex;

        private     List<CloseCursorsTask>          _closeCursors;
        private     int                             closeCursorsIndex;

        private     HashSet<T>                      _autos;

        private     ReserveKeysTask<TKey,T>         _reserveKeys;

        private     Dictionary<JsonKey, Peer<T>>    _creates;
        private     List<WriteTask>                 _createTasks;

        private     Dictionary<JsonKey, Peer<T>>    _upserts;
        private     List<WriteTask>                 _upsertTasks;

        private     Dictionary<JsonKey, EntityPatch>_patches;
        private     List<PatchTask<T>>              _patchTasks;
        private     List<DetectPatchesTask<T>>      _detectPatchesTasks;

        private     HashSet<TKey>                   _deletes;
        private     List<DeleteTask<TKey, T>>       _deleteTasks;

        // --- lazy-initialized getters => they behave like readonly fields
        private     List<ReadTask<TKey, T>>         Reads()         => _readTasks   ?? (_readTasks  = new List<ReadTask<TKey, T>>());

        private     List<QueryTask<T>>              Queries()       => _queryTasks  ?? (_queryTasks = new List<QueryTask<T>>());

        private     List<AggregateTask>             Aggregates()    => _aggregateTasks?? (_aggregateTasks  = new List<AggregateTask>());

        private     List<CloseCursorsTask>          CloseCursors()  =>_closeCursors ?? (_closeCursors= new List<CloseCursorsTask>());

        private     SubscribeChangesTask<T>         subscribeChanges;

        private     HashSet<T>                      Autos()         => _autos       ?? (_autos       = new HashSet<T>(EntityEqualityComparer<T>.Instance));

        private     Dictionary<JsonKey, Peer<T>>    Creates()       => _creates     ?? (_creates     = new Dictionary<JsonKey, Peer<T>>(JsonKey.Equality));
        private     List<WriteTask>                 CreateTasks()   => _createTasks ?? (_createTasks = new List<WriteTask>());

        private     Dictionary<JsonKey, Peer<T>>    Upserts()       => _upserts     ?? (_upserts     = new Dictionary<JsonKey, Peer<T>>(JsonKey.Equality));
        private     List<WriteTask>                 UpsertTasks()   => _upsertTasks ?? (_upsertTasks = new List<WriteTask>());

        private     Dictionary<JsonKey, EntityPatch>Patches()           => _patches            ?? (_patches           = new Dictionary<JsonKey, EntityPatch>(JsonKey.Equality));
        private     List<PatchTask<T>>              PatchTasks()        => _patchTasks         ?? (_patchTasks         = new List<PatchTask<T>>());
        private     List<DetectPatchesTask<T>>      DetectPatchesTasks()=> _detectPatchesTasks ?? (_detectPatchesTasks = new List<DetectPatchesTask<T>>());

        private     HashSet<TKey>                   Deletes()       => _deletes     ?? (_deletes     = CreateHashSet<TKey>(0));
        private     List<DeleteTask<TKey, T>>       DeleteTasks()   => _deleteTasks ?? (_deleteTasks = new List<DeleteTask<TKey, T>>());

        private     DeleteAllTask<TKey, T>          _deleteTaskAll;
        

        internal SyncSet(EntitySet<TKey, T> set) {
            this.set = set;
        }
        internal  override  EntitySet EntitySet => set;

        internal override void AddCreate (Peer<T> peer) {
            Creates().TryAdd(peer.id, peer);    // sole place a peer (entity) is added
            peer.state = PeerState.Created;     // sole place Created is set
        }

        internal override void AddUpsert (Peer<T> peer) {
            Upserts().TryAdd(peer.id, peer);    // sole place a peer (entity) is added
            peer.state = PeerState.Updated;     // sole place Updated is set
        }

        internal void AddDelete (TKey id) {
            Deletes().Add(id);
        }

        internal void AddDeleteRange (ICollection<TKey> keys) {
            var deletes = Deletes();
            deletes.EnsureCapacity(deletes.Count + keys.Count);
            deletes.UnionWith(keys);
        }

        // --- Read
        internal ReadTask<TKey, T> Read() {
            var read = new ReadTask<TKey, T>(set);
            Reads().Add(read);
            return read;
        }

        // --- Query
        internal QueryTask<T> QueryFilter(FilterOperation filter) {
            var queries = Queries();
            var query   = new QueryTask<T>(filter, set.intern.store);
            queries.Add(query);
            return query;
        }

        internal CloseCursorsTask CloseCursors(IEnumerable<string> cursors) {
            var closeCursors = CloseCursors();
            var closeCursor = new CloseCursorsTask(cursors);
            closeCursors.Add(closeCursor);
            return closeCursor;
        }

        // --- Aggregate
        internal CountTask<T> CountFilter(FilterOperation filter) {
            var aggregates  = Aggregates();
            var aggregate   = new CountTask<T>(filter);
            aggregates.Add(aggregate);
            return  aggregate;
        }

        // --- SubscribeChanges
        internal SubscribeChangesTask<T> SubscribeChangesFilter(Change change, FilterOperation filter) {
            if (subscribeChanges == null)
                subscribeChanges = new SubscribeChangesTask<T>();
            var changes = ChangeFlags.ToList(change);
            subscribeChanges.Set(changes, filter);
            return subscribeChanges;
        }

        // --- ReserveKeys
        internal ReserveKeysTask<TKey, T> ReserveKeys(int count) {
            var reserve = _reserveKeys;
            if (reserve == null) {
                return _reserveKeys = new ReserveKeysTask<TKey,T>(count);
            }
            reserve.count += count;
            return reserve;
        }

        // --- Create
        internal CreateTask<T> Create(T entity) {
            if (set.intern.autoIncrement) {
                set.NewEntities().Add(entity);
                Autos().Add(entity);
                var create1 = new CreateTask<T>(new List<T>{entity}, set);
                CreateTasks().Add(create1);
                return create1;
            }
            var peer = set.CreatePeer(entity);
            AddCreate(peer);
            var create = new CreateTask<T>(new List<T>{entity}, set);
            CreateTasks().Add(create);
            return create;
        }

        internal CreateTask<T> CreateRange(ICollection<T> entities) {
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                AddCreate(peer);
            }
            var create = new CreateTask<T>(entities.ToList(), set);
            CreateTasks().Add(create);
            return create;
        }

        // --- Upsert
        internal UpsertTask<T> Upsert(T entity) {
            var peer = set.CreatePeer(entity);
            AddUpsert(peer);
            var upsert = new UpsertTask<T>(new List<T>{entity}, set);
            UpsertTasks().Add(upsert);
            return upsert;
        }

        internal UpsertTask<T> UpsertRange(ICollection<T> entities) {
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                AddUpsert(peer);
            }
            var upsert = new UpsertTask<T>(entities.ToList(), set);
            UpsertTasks().Add(upsert);
            return upsert;
        }

        // --- Delete
        internal DeleteTask<TKey, T> Delete(TKey key) {
            AddDelete(key);
            var delete = new DeleteTask<TKey, T>(new List<TKey>{key}, this);
            DeleteTasks().Add(delete);
            return delete;
        }

        internal DeleteTask<TKey, T> DeleteRange(ICollection<TKey> keys) {
            AddDeleteRange(keys);
            var delete = new DeleteTask<TKey, T>(keys.ToList(), this);
            DeleteTasks().Add(delete);
            return delete;
        }

        internal DeleteAllTask<TKey, T> DeleteAll() {
            if (_deleteTaskAll != null)
                return _deleteTaskAll;
            _deleteTaskAll = new DeleteAllTask<TKey, T>();
            return _deleteTaskAll;
        }
        
        // --- Patch
        // - assign patches
        internal PatchTask<T> Patch(MemberSelection<T> member) {
            var patchTask  = new PatchTask<T>(this, member);
            PatchTasks().Add(patchTask);
            return patchTask;
        }

        // - detect patches
        internal void AddDetectPatches(DetectPatchesTask<T> detectPatchesTask) {
            DetectPatchesTasks().Add(detectPatchesTask);
        }

        // Deprecated comment - preserve for now to remember history of Ref{TKey,T} and Tracer
        //   In case the given entity is already <see cref="Peer{T}.created"/> or <see cref="Peer{T}.updated"/> trace
        //   the entity to find changes in referenced entities in <see cref="Ref{TKey,T}"/> fields of the given entity.
        //   In these cases <see cref="Map.RefMapper{TKey,T}.Trace"/> add untracked entities (== have no <see cref="Peer{T}"/>)
        //   which is not already assigned)
        internal void DetectPeerPatches(Peer<T> peer, DetectPatchesTask<T> detectPatchesTask, ObjectMapper mapper) {
            if ((peer.state & (PeerState.Created | PeerState.Updated)) != 0) {
                // tracer.Trace(peer.Entity);
                return;
            }
            var patchSource = peer.PatchSource;
            if (patchSource == null)
                return;
            var entity  = peer.Entity;
            var patcher = set.intern.store._intern.ObjectPatcher();
            var diff    = patcher.differ.GetDiff(patchSource, entity, mapper.writer);
            if (diff == null)
                return;
            var patches     = Patches();
            var patchList   = patcher.CreatePatches(diff, mapper);
            var id          = peer.id;
            SetNextPatchSource(peer, mapper); // todo next patch source need to be set on Synchronize()
            if (patches.TryGetValue(id, out var entityPatch)) {
                entityPatch.patches.AddRange(patchList);
            } else{
                entityPatch = new EntityPatch { id = id, patches = patchList };
                patches[id] = entityPatch;
            }
            detectPatchesTask.AddPatch(entityPatch, entity);
            // tracer.Trace(entity);
        }

        // ----------------------------------- add tasks methods -----------------------------------
        internal override void AddTasks(List<SyncRequestTask> tasks, ObjectMapper mapper) {
            CloseCursors        (tasks);
            ReserveKeys         (tasks);
            // --- mutate tasks
            CreateEntities      (tasks, mapper);
            UpsertEntities      (tasks, mapper);
            PatchEntities       (tasks);
            DeleteEntities      (tasks);
            DeleteAll           (tasks);
            // --- read tasks
            ReadEntities        (tasks);
            QueryEntities       (tasks);
            AggregateEntities   (tasks);
            SubscribeChanges    (tasks);
        }


        private void ReserveKeys(List<SyncRequestTask> tasks) {
            if (_reserveKeys == null)
                return;
            var req = new ReserveKeys {
                container   = set.name,
                count       = _reserveKeys.count,
            };
            tasks.Add(req);
        }

        private void CreateEntities(List<SyncRequestTask> tasks, ObjectMapper mapper) {
            var createCount = _creates?.Count   ?? 0;
            var autoCount   = _autos?.Count     ?? 0;
            var count       = createCount + autoCount;
            if (count == 0)
                return;
            var entries = new List<JsonValue>   (count);
            var keys    = new List<JsonKey>     (count);
            var writer  = mapper.writer;
            writer.Pretty           = set.intern.writePretty;
            writer.WriteNullMembers = set.intern.writeNull;
            if (_creates  != null) {
                foreach (var createPair in _creates) {
                    T entity    = createPair.Value.Entity;
                    var json    = writer.WriteAsArray(entity);
                    var entry   = new JsonValue(json);
                    var id      = EntityKeyTMap.GetId(entity);
                    entries.Add(entry);
                    keys.Add(id);
                }
            }
            if (_autos  != null) {
                long autoId = -1;   // todo use reserved keys
                foreach (var entity in _autos) {
                    var id      = new JsonKey(autoId);
                    var key     = KeyConvert.IdToKey(id);
                    EntityKeyTMap.SetKey(entity, key);
                    var json    = writer.WriteAsArray(entity);
                    var entry   = new JsonValue(json);
                    entries.Add(entry);
                    keys.Add(id);
                }
            }
            var req = new CreateEntities {
                container       = set.name,
                keyName         = SyncKeyName(set.GetKeyName()),
                entities        = entries,
                entityKeys      = keys,
                reservedToken   = new Guid() // todo
            };
            tasks.Add(req);
        }

        private void UpsertEntities(List<SyncRequestTask> tasks, ObjectMapper mapper) {
            if (_upserts == null || _upserts.Count == 0)
                return;
            var writer              = mapper.writer;
            writer.Pretty           = set.intern.writePretty;
            writer.WriteNullMembers = set.intern.writeNull;
            var entries = new List<JsonValue>   (_upserts.Count);
            var keys    = new List<JsonKey>    (_upserts.Count);

            foreach (var upsertPair in _upserts) {
                T entity    = upsertPair.Value.Entity;
                var json    = writer.WriteAsArray(entity);
                var entry   = new JsonValue(json);
                var id      = EntityKeyTMap.GetId(entity);
                entries.Add(entry);
                keys.Add(id);
            }
            var req = new UpsertEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                entities    = entries,
                entityKeys  = keys
            };
            tasks.Add(req);
        }

        private void ReadEntities(List<SyncRequestTask> tasks) {
            if (_readTasks == null || _readTasks.Count == 0)
                return;

            foreach (var read in _readTasks) {
                List<References> references = null;
                if (read.relations.subRelations.Count > 0) {
                    references = new List<References>(_readTasks.Count);
                    AddReferences(references, read.relations.subRelations);
                }
                var ids = new List<JsonKey>(read.result.Keys.Count);
                foreach (var key in read.result.Keys) {
                    var id = KeyConvert.KeyToId(key);
                    ids.Add(id);
                }
                var readList = new ReadEntities {
                    container   = set.name,
                    keyName     = SyncKeyName(set.GetKeyName()),
                    isIntKey    = IsIntKey(set.IsIntKey()),
                    ids         = ids,
                    references  = references
                };
                tasks.Add(readList);
            }
        }

        private void QueryEntities(List<SyncRequestTask> tasks) {
            if (_queryTasks == null || _queryTasks.Count == 0)
                return;
            foreach (var query in _queryTasks) {
                var subRelations = query.relations.subRelations;
                List<References> references = null;
                if (subRelations.Count > 0) {
                    references = new List<References>(subRelations.Count);
                    AddReferences(references, subRelations);
                }
                var queryFilter = query.filter;
                if (query.filter is Filter filter) {
                    queryFilter = filter.body;
                }
                var filterTree  = FilterToJson(queryFilter);
                var req = new QueryEntities {
                    container   = set.name,
                    keyName     = SyncKeyName(set.GetKeyName()),
                    isIntKey    = IsIntKey(set.IsIntKey()),
                    filterTree  = filterTree,
                    filter      = query.filterLinq,
                    references  = references,
                    limit       = query.limit,
                    maxCount    = query.maxCount,
                    cursor      = query.cursor
                };
                tasks.Add(req);
            }
        }

        private void AggregateEntities(List<SyncRequestTask> tasks) {
            if (_aggregateTasks == null || _aggregateTasks.Count == 0)
                return;
            foreach (var aggregate in _aggregateTasks) {
                var aggregateFilter = aggregate.filter;
                if (aggregate.filter is Filter filter) {
                    aggregateFilter = filter.body;
                }
                var filterTree  = FilterToJson(aggregateFilter);
                var req = new AggregateEntities {
                    container   = set.name,
                    type        = aggregate.Type,
                //  keyName     = SyncKeyName(set.GetKeyName()),
                //  isIntKey    = IsIntKey(set.IsIntKey()),
                    filterTree  = filterTree,
                    filter      = aggregate.filterLinq
                };
                tasks.Add(req);
            }
        }
        
        private JsonValue FilterToJson(FilterOperation filter) {
            using (var pooled = set.intern.store.ObjectMapper.Get()) {
                var writer      = pooled.instance.writer;
                var jsonFilter  = writer.Write(filter);
                return new JsonValue(jsonFilter);
            }
        }

        private void CloseCursors(List<SyncRequestTask> tasks) {
            if (_closeCursors == null || _closeCursors.Count == 0)
                return;
            foreach (var closeCursor in _closeCursors) {
                var req = new CloseCursors {
                    container   = set.name,
                    cursors     = closeCursor.cursors
                };
                tasks.Add(req);
            }
        }
        
        internal override void AddEntityPatches(PatchTask<T> patchTask, ICollection<T> entities) {
            using (var pooled = set.intern.store.ObjectMapper.Get()) {
                var mapper          = pooled.instance;
                // todo performance: cache MemberAccess instances with members as key
                var members         = patchTask.selection.Members;
                var memberAccess    = patchTask.selection.GetMemberAccess();
                var memberAccessor  = new MemberAccessor(mapper.writer);
                var entityPatches   = Patches();
                var taskPatches     = patchTask.patches;
                // taskPatches.Capacity= taskPatches.Count + entities.Count;    -> degrade performance

                foreach (var entity in entities) {
                    var id = EntityKeyTMap.GetId(entity);
                    if (!entityPatches.TryGetValue(id, out EntityPatch patch)) {
                        patch = new EntityPatch { id = id, patches = new List<JsonPatch>() };
                        entityPatches.Add(id, patch);
                    }
                    var patchInfo = new EntityPatchInfo<T>(patch, entity);
                    taskPatches.Add(patchInfo);
                    var key = KeyConvert.IdToKey(id);
                    if (set.TryGetPeerByKey(key, out var peer)) {
                        SetNextPatchSource(peer, mapper);
                    }
                    var patches         = patch.patches;
                    var selectResults   = memberAccessor.GetValues(entity, memberAccess);
                    int n = 0;
                    foreach (var path in members) {
                        var value = selectResults[n++].Json;
                        patches.Add(new PatchReplace { path = path, value = value });
                    }
                }
            }
        }

        private void PatchEntities(List<SyncRequestTask> tasks)
        {
            var patches     = _patches;
            if (patches != null && patches.Count > 0) {
                var list = new List<EntityPatch>(patches.Count);
                foreach (var pair in patches) { list.Add(pair.Value); }
                var req = new PatchEntities {
                    container   = set.name,
                    keyName     = SyncKeyName(set.GetKeyName()),
                    patches     = list
                };
                tasks.Add(req);
            }
            if (_patchTasks != null) {
                foreach (var task in _patchTasks) {
                    if (task.patches.Count == 0)
                        task.state.Executed = true;
                }
            }
            if (_detectPatchesTasks != null) {
                foreach (var task in _detectPatchesTasks) {
                    if (task.Patches.Count == 0)
                        task.state.Executed = true;
                }
            }
        }

        private void DeleteEntities(List<SyncRequestTask> tasks) {
            var deletes = _deletes;
            if (deletes == null || deletes.Count == 0)
                return;
            var ids = new List<JsonKey>(deletes.Count);
            foreach (var key in deletes) {
                var id = KeyConvert.KeyToId(key);
                ids.Add(id);
            }
            var req = new DeleteEntities {
                container   = set.name,
                ids         = ids
            };
            tasks.Add(req);
            deletes.Clear();
        }

        private void DeleteAll(List<SyncRequestTask> tasks) {
            var deleteAll = _deleteTaskAll;
            if (deleteAll == null)
                return;
            var req = new DeleteEntities {
                container   = set.name,
                all         = true
            };
            tasks.Add(req);
        }

        private void SubscribeChanges(List<SyncRequestTask> tasks) {
            var sub = subscribeChanges;
            if (sub == null)
                return;
            var filterJson = FilterToJson(sub.filter);
            var req = new SubscribeChanges {
                container   = set.name,
                filter      = filterJson,
                changes     = sub.changes
            };
            tasks.Add(req);
        }

        // ----------------------------------- helper methods -----------------------------------
        private static void AddReferences(List<References> references, SubRelations relations) {
            foreach (var readRefs in relations) {
                var queryReference = new References {
                    container   = readRefs.Container,
                    keyName     = SyncKeyName(readRefs.KeyName),
                    isIntKey    = IsIntKey(readRefs.IsIntKey),
                    selector    = readRefs.Selector
                };
                references.Add(queryReference);
                var subRefsMap = readRefs.SubRelations;
                if (subRefsMap.Count > 0) {
                    queryReference.references = new List<References>(subRefsMap.Count);
                    AddReferences(queryReference.references, subRefsMap);
                }
            }
        }

        private static void SetNextPatchSource(Peer<T> peer, ObjectMapper mapper) {
            var jsonArray   = mapper.writer.WriteAsArray(peer.Entity);
            var json        = new JsonValue(jsonArray);
            peer.SetNextPatchSource(mapper.Read<T>(json));
        }

        internal void SetTaskInfo(ref SetInfo info) {
            info.tasks =
                (_reserveKeys   != null ? 1 : 0) +
                SetInfo.Count(_readTasks)       +
                SetInfo.Count(_queryTasks)     +
                SetInfo.Count(_aggregateTasks)  +
                SetInfo.Count(_closeCursors)+
                SetInfo.Count(_createTasks) +  SetInfo.Any  (_autos) +
                SetInfo.Count(_upsertTasks) +
                SetInfo.Any  (_patches)     +
                SetInfo.Count(_deleteTasks) +
                (_deleteTaskAll   != null ? 1 : 0) +
                (subscribeChanges != null ? 1 : 0);
            //
            info.reads          = SetInfo.Count(_readTasks);
            info.queries        = SetInfo.Count(_queryTasks);
            info.aggregates     = SetInfo.Count(_aggregateTasks);
            info.closeCursors   = SetInfo.Count(_closeCursors);
            info.create         = SetInfo.Count(_createTasks) + SetInfo.Count(_autos);
            info.upsert         = SetInfo.Count(_upsertTasks);
            info.patch          = SetInfo.Count(_patches);
            info.delete         = SetInfo.Count(_deleteTasks);
            // info.readRefs    = readRefsMap.Count;
        }
    }
}