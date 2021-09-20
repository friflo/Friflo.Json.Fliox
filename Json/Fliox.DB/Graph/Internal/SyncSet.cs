// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.DB.Graph.Internal.KeyEntity;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    internal abstract class SyncSetBase <T> : SyncSet where T : class
    {
        internal abstract void AddUpsert (Peer<T> peer);
        internal abstract bool AddCreate (Peer<T> peer);
    }

    /// Multiple instances of this class can be created when calling EntitySet.Sync() without awaiting the result.
    /// Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal partial class SyncSet<TKey, T> : SyncSetBase<T> where T : class
    {
        private     static readonly EntityKeyT<TKey, T>     EntityKeyTMap = EntityKey.GetEntityKeyT<TKey, T>();

        // Note!
        // All fields & getters must be private by all means to ensure that all scheduled tasks of a Sync() request
        // managed by this instance can be mapped to their task results safely.
        
        private     readonly EntitySet<TKey, T>             set;
        
        // --- backing fields for lazy-initialized getters
        private     List<ReadTask<TKey, T>>                 _reads;
        
        private     Dictionary<string, QueryTask<TKey, T>>  _queries;
        
        private     HashSet<T>                              _autos;
        
        private     ReserveKeysTask<TKey,T>                 _reserveKeys;
            
        private     Dictionary<JsonKey, Peer<T>>            _creates;
        private     List<WriteTask>                         _createTasks;
        
        private     Dictionary<JsonKey, Peer<T>>            _upserts;
        private     List<WriteTask>                         _upsertTasks;
        
        private     Dictionary<JsonKey, EntityPatch>        _patches;
        private     List<PatchTask<T>>                      _patchTasks;
        
        private     HashSet<TKey>                           _deletes;
        private     List<DeleteTask<TKey, T>>               _deleteTasks;

        // --- lazy-initialized getters => they behave like readonly fields
        private     List<ReadTask<TKey, T>>                 Reads()      => _reads       ?? (_reads       = new List<ReadTask<TKey, T>>());
        
        /// key: <see cref="QueryTask{TKey,T}.filterLinq"/>
        private     Dictionary<string, QueryTask<TKey, T>>  Queries()    => _queries     ?? (_queries     = new Dictionary<string, QueryTask<TKey, T>>());
        
        private     SubscribeChangesTask<T>                 subscribeChanges;

        private     HashSet<T>                              Autos()      => _autos       ?? (_autos        = new HashSet<T>(EntityEqualityComparer<T>.Instance));

        private     Dictionary<JsonKey, Peer<T>>            Creates()    => _creates     ?? (_creates     = new Dictionary<JsonKey, Peer<T>>(JsonKey.Equality));
        private     List<WriteTask>                         CreateTasks()=> _createTasks ?? (_createTasks = new List<WriteTask>());
        
        private     Dictionary<JsonKey, Peer<T>>            Upserts()    => _upserts     ?? (_upserts     = new Dictionary<JsonKey, Peer<T>>(JsonKey.Equality));
        private     List<WriteTask>                         UpsertTasks()=> _upsertTasks ?? (_upsertTasks = new List<WriteTask>());

        private     Dictionary<JsonKey, EntityPatch>        Patches()    => _patches     ?? (_patches     = new Dictionary<JsonKey, EntityPatch>(JsonKey.Equality));
        private     List<PatchTask<T>>                      PatchTasks() => _patchTasks  ?? (_patchTasks  = new List<PatchTask<T>>());
        
        private     HashSet<TKey>                           Deletes()    => _deletes     ?? (_deletes     = new HashSet   <TKey>());
        private     List<DeleteTask<TKey, T>>               DeleteTasks()=> _deleteTasks ?? (_deleteTasks = new List<DeleteTask<TKey, T>>());
        
        private     DeleteAllTask<TKey, T>                  _deleteTaskAll;

        internal SyncSet(EntitySet<TKey, T> set) {
            this.set = set;
        }
        
        internal override bool AddCreate (Peer<T> peer) {
            peer.assigned = true;
            Creates().TryAdd(peer.id, peer);    // sole place a peer (entity) is added
            if (!peer.created) {
                peer.created = true;            // sole place created set to true
                return true;
            }
            return false;
        }
        
        internal override void AddUpsert (Peer<T> peer) {
            peer.assigned = true;
            Upserts().TryAdd(peer.id, peer);      // sole place a peer (entity) is added
            if (!peer.updated) {
                peer.updated = true;            // sole place created set to true
            }
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
        internal QueryTask<TKey, T> QueryFilter(FilterOperation filter) {
            var filterLinq = filter.Linq;
            var queries = Queries(); 
            if (queries.TryGetValue(filterLinq, out QueryTask<TKey, T> query))
                return query;
            query = new QueryTask<TKey, T>(filter, set.intern.store);
            queries.Add(filterLinq, query);
            return query;
        }
        
        // --- SubscribeChanges
        internal SubscribeChangesTask<T> SubscribeChangesFilter(IEnumerable<Change> changes, FilterOperation filter) {
            if (subscribeChanges == null)
                subscribeChanges = new SubscribeChangesTask<T>();
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
        
        // --- Patch
        internal PatchTask<T> Patch(Peer<T> peer) {
            var patchTask  = new PatchTask<T>(peer, set);
            PatchTasks().Add(patchTask);
            return patchTask;
        }
        
        internal PatchTask<T> PatchRange(ICollection<Peer<T>> peers) {
            var patchTask  = new PatchTask<T>(peers, set);
            PatchTasks().Add(patchTask);
            return patchTask;
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
        
        // --- Log changes -> create patches
        internal void LogSetChanges(Dictionary<TKey, Peer<T>> peers, LogTask logTask) {
            foreach (var peerPair in peers) {
                Peer<T> peer = peerPair.Value;
                GetEntityChanges(peer, logTask);
            }
        }

        internal void LogEntityChanges(T entity, LogTask logTask) {
            var peer = set.GetPeerByEntity(entity);
            GetEntityChanges(peer, logTask);
        }

        /// In case the given entity is already <see cref="Peer{T}.created"/> or <see cref="Peer{T}.updated"/> trace
        /// the entity to find changes in referenced entities in <see cref="Ref{TKey,T}"/> fields of the given entity.
        /// In these cases <see cref="Map.RefMapper{TKey,T}.Trace"/> add untracked entities (== have no <see cref="Peer{T}"/>)
        /// which is not already assigned) 
        private void GetEntityChanges(Peer<T> peer, LogTask logTask) {
            ref var intern = ref set.intern;
            if (peer.created || peer.updated) {
                intern.store._intern.tracerLogTask = logTask;
                intern.tracer.Trace(peer.Entity);
                return;
            }
            var patchSource = peer.PatchSource;
            if (patchSource != null) {
                var entity = peer.Entity;
                var diff = intern.objectPatcher.differ.GetDiff(patchSource, entity);
                if (diff == null)
                    return;
                var patchList = intern.objectPatcher.CreatePatches(diff);
                var entityPatch = new EntityPatch {
                    patches = patchList
                };
                SetNextPatchSource(peer); // todo next patch source need to be set on Sync() 
                var id = peer.id;
                Patches()[id] = entityPatch;
                logTask.AddPatch(this, id);
                
                intern.store._intern.tracerLogTask = logTask;
                intern.tracer.Trace(entity);
            }
        }

        // ----------------------------------- add tasks methods -----------------------------------
        internal override void AddTasks(List<DatabaseTask> tasks) {
            ReserveKeys         (tasks);
            CreateEntities      (tasks);
            UpsertEntities      (tasks);
            ReadEntitiesList    (tasks);
            QueryEntities       (tasks);
            PatchEntities       (tasks);
            DeleteEntities      (tasks);
            DeleteAll           (tasks);
            SubscribeChanges    (tasks);
        }
        
        
        private void ReserveKeys(List<DatabaseTask> tasks) {
            if (_reserveKeys == null)
                return;
            var req = new ReserveKeys {
                container   = set.name,
                count       = _reserveKeys.count,
            };
            tasks.Add(req);
        }
        
        private void CreateEntities(List<DatabaseTask> tasks) {
            var createCount = _creates?.Count   ?? 0; 
            var autoCount   = _autos?.Count     ?? 0;
            var count       = createCount + autoCount;
            if (count == 0)
                return;
            var entries = new List<JsonValue>   (count);
            var keys    = new List<JsonKey>     (count);
            var writer  = set.intern.jsonMapper.writer;
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
                    var key     = Ref<TKey,T>.RefKeyMap.IdToKey(id);
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
        
        private void UpsertEntities(List<DatabaseTask> tasks) {
            if (_upserts == null || _upserts.Count == 0)
                return;
            var writer  = set.intern.jsonMapper.writer;
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

        private void ReadEntitiesList(List<DatabaseTask> tasks) {
            if (_reads == null || _reads.Count == 0)
                return;
            var readList = new ReadEntitiesList {
                reads       = new List<ReadEntities>(_reads.Count),
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                isIntKey    = IsIntKey(set.IsIntKey())
            };
            foreach (var read in _reads) {
                List<References> references = null;
                if (read.refsTask.subRefs.Count > 0) {
                    references = new List<References>(_reads.Count);
                    AddReferences(references, read.refsTask.subRefs);
                }
                var ids = Helper.CreateHashSet(read.results.Keys.Count, JsonKey.Equality);
                foreach (var key in read.results.Keys) {
                    var id = Ref<TKey,T>.RefKeyMap.KeyToId(key);
                    ids.Add(id);
                }
                var req = new ReadEntities {
                    ids         = ids,
                    references  = references
                };
                readList.reads.Add(req);
            }
            tasks.Add(readList);
        }
        
        private void QueryEntities(List<DatabaseTask> tasks) {
            if (_queries == null || _queries.Count == 0)
                return;
            foreach (var queryPair in _queries) {
                QueryTask<TKey, T> query = queryPair.Value;
                var subRefs = query.refsTask.subRefs;
                List<References> references = null;
                if (subRefs.Count > 0) {
                    references = new List<References>(subRefs.Count);
                    AddReferences(references, subRefs);
                }
                var req = new QueryEntities {
                    container   = set.name,
                    keyName     = SyncKeyName(set.GetKeyName()),
                    isIntKey    = IsIntKey(set.IsIntKey()),  
                    filter      = query.filter,
                    filterLinq  = query.filterLinq,
                    references  = references
                };
                tasks.Add(req);
            }
        }

        private void PatchEntities(List<DatabaseTask> tasks)
        {
            var patches     = _patches;
            var patchTasks  = _patchTasks;
            
            if (patchTasks != null && patchTasks.Count > 0) {
                patches = Patches();    
                foreach (var patchTask in patchTasks) {
                    // todo performance: cache MemberAccess instances with members as key
                    var memberAccess    = new MemberAccess(patchTask.members);
                    var memberAccessor  = new MemberAccessor(set.intern.store._intern.jsonMapper.writer);
                    
                    foreach (var peer in patchTask.peers) {
                        var entity  = peer.Entity;
                        var id      = peer.id;
                        if (!patches.TryGetValue(id, out EntityPatch patch)) {
                            patch = new EntityPatch {
                                patches = new List<JsonPatch>()
                            };
                            patches.Add(id, patch);
                            SetNextPatchSource(peer);
                        }
                        var entityPatches   = patch.patches;
                        var selectResults   = memberAccessor.GetValues(entity, memberAccess);
                        int n = 0;
                        foreach (var path in patchTask.members) {
                            var value = new JsonValue {
                                json = selectResults[n++].Json
                            };
                            entityPatches.Add(new PatchReplace {
                                path = path,
                                value = value
                            });
                        }
                    }
                }
            }
            if (patches != null && patches.Count > 0) {
                var req = new PatchEntities {
                    container   = set.name,
                    keyName     = SyncKeyName(set.GetKeyName()),
                    patches     = new Dictionary<JsonKey, EntityPatch>(patches, JsonKey.Equality)
                };
                tasks.Add(req);
            }
        }

        private void DeleteEntities(List<DatabaseTask> tasks) {
            var deletes = _deletes;
            if (deletes == null || deletes.Count == 0)
                return;
            var ids = Helper.CreateHashSet (deletes.Count, JsonKey.Equality);
            foreach (var key in deletes) {
                var id = Ref<TKey,T>.RefKeyMap.KeyToId(key);
                ids.Add(id);
            }
            var req = new DeleteEntities {
                container   = set.name,
                ids         = ids
            };
            tasks.Add(req);
            deletes.Clear();
        }
        
        private void DeleteAll(List<DatabaseTask> tasks) {
            var deleteAll = _deleteTaskAll;
            if (deleteAll == null)
                return;
            var req = new DeleteEntities {
                container   = set.name,
                all         = true
            };
            tasks.Add(req);
        }
        
        private void SubscribeChanges(List<DatabaseTask> tasks) {
            var sub = subscribeChanges;
            if (sub == null)
                return;
            var req = new SubscribeChanges {
                container   = set.name,
                filter      = sub.filter,
                changes     = sub.changes
            };
            tasks.Add(req);
        }
        
        // ----------------------------------- helper methods -----------------------------------
        private static void AddReferences(List<References> references, SubRefs refs) {
            foreach (var readRefs in refs) {
                var queryReference = new References {
                    container   = readRefs.Container,
                    keyName     = SyncKeyName(readRefs.KeyName),
                    isIntKey    = IsIntKey(readRefs.IsIntKey), 
                    selector    = readRefs.Selector
                };
                references.Add(queryReference);
                var subRefsMap = readRefs.SubRefs;
                if (subRefsMap.Count > 0) {
                    queryReference.references = new List<References>(subRefsMap.Count);
                    AddReferences(queryReference.references, subRefsMap);
                }
            }
        }
        
        private void SetNextPatchSource(Peer<T> peer) {
            var mapper      = set.intern.jsonMapper;
            var jsonArray   = mapper.writer.WriteAsArray(peer.Entity);
            var json        = new Utf8Json(jsonArray);
            peer.SetNextPatchSource(mapper.Read<T>(json));
        }

        internal void SetTaskInfo(ref SetInfo info) {
            info.tasks =
                (_reserveKeys   != null ? 1 : 0) +
                SetInfo.Any  (_reads)   +
                SetInfo.Count(_queries) +
                SetInfo.Any  (_creates) +  SetInfo.Any  (_autos) +
                SetInfo.Any  (_upserts) +
                SetInfo.Any  (_patches) + SetInfo.Any(_patchTasks) +
                SetInfo.Any  (_deletes) +
                (_deleteTaskAll   != null ? 1 : 0) +
                (subscribeChanges != null ? 1 : 0);
            //
            info.reads      = SetInfo.Count(_reads);
            info.queries    = SetInfo.Count(_queries);
            info.create     = SetInfo.Count(_creates) + SetInfo.Count(_autos);
            info.upsert     = SetInfo.Count(_upserts);
            info.patch      = SetInfo.Count(_patches) + SetInfo.Count(_patchTasks);
            info.delete     = SetInfo.Count(_deletes);
            // info.readRefs   = readRefsMap.Count;
        }
    }
}