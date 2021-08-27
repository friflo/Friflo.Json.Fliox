// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal abstract class SyncPeerSet <T> : SyncSet where T : class
    {
        internal abstract void AddUpdate (Peer<T> peer);
        internal abstract bool AddCreate (Peer<T> peer);
    }

    /// Multiple instances of this class can be created when calling EntitySet.Sync() without awaiting the result.
    /// Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal partial class SyncSet<TKey, T> : SyncPeerSet<T> where T : class
    {
        // Note!
        // All fields must be private by all means to ensure that all scheduled tasks of a Sync() request managed
        // by this instance can be mapped to their task results safely.
        
        private readonly    EntitySet<TKey, T>                      set;
            
        private readonly    List<ReadTask<TKey, T>>                 reads        = new List<ReadTask<TKey, T>>();
        /// key: <see cref="QueryTask{TKey,T}.filterLinq"/> 
        private readonly    Dictionary<string, QueryTask<TKey, T>>  queries      = new Dictionary<string, QueryTask<TKey, T>>();
        
        private             SubscribeChangesTask<T>                 subscribeChanges;
        
        /// key: <see cref="Peer{T}.entity"/>.id
        private readonly    Dictionary<JsonKey, Peer<T>>            creates      = new Dictionary<JsonKey, Peer<T>>(JsonKey.Equality);
        private readonly    List<WriteTask>                         createTasks  = new List<WriteTask>();
        
        /// key: <see cref="Peer{T}.entity"/>.id
        private readonly    Dictionary<JsonKey, Peer<T>>            updates      = new Dictionary<JsonKey, Peer<T>>(JsonKey.Equality);
        private readonly    List<WriteTask>                         updateTasks  = new List<WriteTask>();

        /// key: entity id
        private readonly    Dictionary<JsonKey, EntityPatch>        patches      = new Dictionary<JsonKey, EntityPatch>(JsonKey.Equality);
        private readonly    List<PatchTask<T>>                      patchTasks   = new List<PatchTask<T>>();
        
        /// key: entity id
        private readonly    HashSet<TKey>                           deletes      = new HashSet   <TKey>();
        private readonly    List<DeleteTask<TKey, T>>               deleteTasks  = new List<DeleteTask<TKey, T>>();

        internal SyncSet(EntitySet<TKey, T> set) {
            this.set = set;
        }
        
        internal override bool AddCreate (Peer<T> peer) {
            peer.assigned = true;
            creates.TryAdd(peer.id, peer);      // sole place a peer (entity) is added
            if (!peer.created) {
                peer.created = true;            // sole place created set to true
                return true;
            }
            return false;
        }
        
        internal override void AddUpdate (Peer<T> peer) {
            peer.assigned = true;
            updates.TryAdd(peer.id, peer);      // sole place a peer (entity) is added
            if (!peer.updated) {
                peer.updated = true;            // sole place created set to true
            }
        }
        
        internal void AddDelete (TKey id) {
            deletes.Add(id);
        }
        
        // --- Read
        internal ReadTask<TKey, T> Read() {
            var read = new ReadTask<TKey, T>(set);
            reads.Add(read);
            return read;
        }
        
        // --- Query
        internal QueryTask<TKey, T> QueryFilter(FilterOperation filter) {
            var filterLinq = filter.Linq;
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
        
        // --- Create
        internal CreateTask<T> Create(T entity) {
            var peer = set.CreatePeer(entity);
            AddCreate(peer);
            var create = new CreateTask<T>(new List<T>{entity}, set);
            createTasks.Add(create);
            return create;
        }
        
        internal CreateTask<T> CreateRange(ICollection<T> entities) {
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                AddCreate(peer);
            }
            var create = new CreateTask<T>(entities.ToList(), set);
            createTasks.Add(create);
            return create;
        }
        
        // --- Update
        internal UpdateTask<T> Update(T entity) {
            var peer = set.CreatePeer(entity);
            AddUpdate(peer);
            var update = new UpdateTask<T>(new List<T>{entity}, set);
            updateTasks.Add(update);
            return update;
        }
        
        internal UpdateTask<T> UpdateRange(ICollection<T> entities) {
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                AddUpdate(peer);
            }
            var update = new UpdateTask<T>(entities.ToList(), set);
            updateTasks.Add(update);
            return update;
        }
        
        // --- Patch
        internal PatchTask<T> Patch(Peer<T> peer) {
            var patchTask  = new PatchTask<T>(peer, set);
            patchTasks.Add(patchTask);
            return patchTask;
        }
        
        internal PatchTask<T> PatchRange(ICollection<Peer<T>> peers) {
            var patchTask  = new PatchTask<T>(peers, set);
            patchTasks.Add(patchTask);
            return patchTask;
        }
        
        // --- Delete
        internal DeleteTask<TKey, T> Delete(TKey key) {
            AddDelete(key);
            var delete = new DeleteTask<TKey, T>(new List<TKey>{key}, set);
            deleteTasks.Add(delete);
            return delete;
        }
        
        internal DeleteTask<TKey, T> DeleteRange(ICollection<TKey> keys) {
            foreach (var key in keys) {
                AddDelete(key);
            }
            var delete = new DeleteTask<TKey, T>(keys.ToList(), set);
            deleteTasks.Add(delete);
            return delete;
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

        /// In case the given entity was added via <see cref="Create"/> (peer.create != null) trace the entity to
        /// find changes in referenced entities in <see cref="Ref{TKey,T}"/> fields of the given entity.
        /// In these cases <see cref="Map.RefMapper{TKey,T}.Trace"/> add untracked entities (== have no <see cref="Peer{T}"/>)
        /// which is not already assigned) 
        private void GetEntityChanges(Peer<T> peer, LogTask logTask) {
            if (peer.created) {
                set.intern.store._intern.tracerLogTask = logTask;
                set.intern.tracer.Trace(peer.Entity);
                return;
            }
            var patchSource = peer.PatchSource;
            if (patchSource != null) {
                var entity = peer.Entity;
                var diff = set.intern.objectPatcher.differ.GetDiff(patchSource, entity);
                if (diff == null)
                    return;
                var patchList = set.intern.objectPatcher.CreatePatches(diff);
                var entityPatch = new EntityPatch {
                    patches = patchList
                };
                SetNextPatchSource(peer); // todo next patch source need to be set on Sync() 
                var id = peer.id;
                patches[id] = entityPatch;
                logTask.AddPatch(this, id);
                
                set.intern.store._intern.tracerLogTask = logTask;
                set.intern.tracer.Trace(entity);
            }
        }

        // ----------------------------------- add tasks methods -----------------------------------
        internal override void AddTasks(List<DatabaseTask> tasks) {
            CreateEntities  (tasks);
            UpdateEntities  (tasks);
            ReadEntitiesList(tasks);
            QueryEntities   (tasks);
            PatchEntities   (tasks);
            DeleteEntities  (tasks);
            SubscribeChanges(tasks);
        }
        
        private void CreateEntities(List<DatabaseTask> tasks) {
            if (creates.Count == 0)
                return;
            var writer = set.intern.jsonMapper.writer;
            var entries = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
            
            foreach (var createPair in creates) {
                T entity    = createPair.Value.Entity;
                var json    = writer.Write(entity);
                var entry   = new EntityValue(json);
                var id      = Ref<TKey,T>.EntityKey.GetId(entity);
                entries.Add(id, entry);
            }
            var req = new CreateEntities {
                container = set.name,
                entities = entries
            };
            tasks.Add(req);
        }

        private void UpdateEntities(List<DatabaseTask> tasks) {
            if (updates.Count == 0)
                return;
            var writer = set.intern.jsonMapper.writer;
            var entries = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
            
            foreach (var updatePair in updates) {
                T entity    = updatePair.Value.Entity;
                var json    = writer.Write(entity);
                var entry   = new EntityValue(json);
                var id      = Ref<TKey,T>.EntityKey.GetId(entity);
                entries.Add(id, entry);
            }
            var req = new UpdateEntities {
                container = set.name,
                entities = entries
            };
            tasks.Add(req);
        }

        private void ReadEntitiesList(List<DatabaseTask> tasks) {
            if (reads.Count == 0)
                return;
            var readList = new ReadEntitiesList {
                reads       = new List<ReadEntities>(),
                container   = set.name
            };
            foreach (var read in reads) {
                List<References> references = null;
                if (read.refsTask.subRefs.Count >= 0) {
                    references = new List<References>(reads.Count);
                    AddReferences(references, read.refsTask.subRefs);
                }
                var ids = Helper.CreateHashSet(read.results.Keys.Count, JsonKey.Equality);
                foreach (var key in read.results.Keys) {
                    var id = Ref<TKey,T>.EntityKey.KeyToId(key);
                    ids.Add(id);
                }
                var req = new ReadEntities {
                    ids = ids,
                    references = references
                };
                readList.reads.Add(req);
            }
            tasks.Add(readList);
        }
        
        private void QueryEntities(List<DatabaseTask> tasks) {
            if (queries.Count == 0)
                return;
            foreach (var queryPair in queries) {
                QueryTask<TKey, T> query = queryPair.Value;
                var subRefs = query.refsTask.subRefs;
                List<References> references = null;
                if (subRefs.Count > 0) {
                    references = new List<References>(subRefs.Count);
                    AddReferences(references, subRefs);
                }
                var req = new QueryEntities {
                    container   = set.name,
                    filter      = query.filter,
                    filterLinq  = query.filterLinq,
                    references  = references
                };
                tasks.Add(req);
            }
        }

        private void PatchEntities(List<DatabaseTask> tasks) {
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
            if (patches.Count > 0) {
                var req = new PatchEntities {
                    container = set.name,
                    patches = new Dictionary<JsonKey, EntityPatch>(patches, JsonKey.Equality)
                };
                tasks.Add(req);
            }
        }

        private void DeleteEntities(List<DatabaseTask> tasks) {
            if (deletes.Count == 0)
                return;
            var ids = Helper.CreateHashSet (deletes.Count, JsonKey.Equality);
            foreach (var key in deletes) {
                var id = Ref<TKey, T>.EntityKey.KeyToId(key);
                ids.Add(id);
            }
            var req = new DeleteEntities {
                container   = set.name,
                ids         = ids
            };
            tasks.Add(req);
            deletes.Clear();
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
                    container = readRefs.Container,
                    selector  = readRefs.Selector
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
            var mapper = set.intern.jsonMapper;
            var json = mapper.writer.Write(peer.Entity);
            peer.SetNextPatchSource(mapper.Read<T>(json));
        }

        private static int Any(int count) { return count != 0 ? 1 : 0; }

        internal void SetTaskInfo(ref SetInfo info) {
            info.tasks =
                Any(reads.Count)   +
                queries.Count       +
                Any(creates.Count) +
                Any(updates.Count) +
                Any(patches.Count + patchTasks.Count) +
                Any(deletes.Count) +
                (subscribeChanges != null ? 1 : 0);
            //
            info.reads      = reads.Count;
            info.queries    = queries.Count;
            info.create     = creates.Count;
            info.update     = updates.Count;
            info.patch      = patches.Count + patchTasks.Count;
            info.delete     = deletes.Count;
            // info.readRefs   = readRefsMap.Count;
        }
    }
}