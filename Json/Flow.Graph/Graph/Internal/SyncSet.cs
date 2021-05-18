// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Graph.Internal.Map;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph.Internal
{

    /// Multiple instances of this class can be created when calling EntitySet.Sync() without awaiting the result.
    /// Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal partial class SyncSet<T> : SyncSet where T : Entity
    {
        // Note!
        // All fields must be private by all means to ensure that all scheduled tasks of a Sync() request managed
        // by this instance can be mapped to their task results safely.
        
        private readonly    EntitySet<T>                        set;
        private readonly    List<string>                        idsBuf       = new List<string>();
            
        private readonly    List<ReadTask<T>>                   reads        = new List<ReadTask<T>>();
        /// key: <see cref="QueryTask{T}.filterLinq"/> 
        private readonly    Dictionary<string, QueryTask<T>>    queries      = new Dictionary<string, QueryTask<T>>();
        
        /// key: <see cref="PeerEntity{T}.entity"/>.id
        private readonly    Dictionary<string, PeerEntity<T>>   creates      = new Dictionary<string, PeerEntity<T>>();
        private readonly    List<WriteTask>                     createTasks  = new List<WriteTask>();
        
        /// key: <see cref="PeerEntity{T}.entity"/>.id
        private readonly    Dictionary<string, PeerEntity<T>>   updates      = new Dictionary<string, PeerEntity<T>>();
        private readonly    List<WriteTask>                     updateTasks  = new List<WriteTask>();

        /// key: entity id
        private readonly    Dictionary<string, EntityPatch>     patches      = new Dictionary<string, EntityPatch>();
        private readonly    List<PatchTask<T>>                  patchTasks   = new List<PatchTask<T>>();
        
        /// key: entity id
        private readonly    HashSet<string>                     deletes      = new HashSet   <string>();
        private readonly    List<DeleteTask<T>>                 deleteTasks  = new List<DeleteTask<T>>();

        internal SyncSet(EntitySet<T> set) {
            this.set = set;
        }
        
        internal bool AddCreate (PeerEntity<T> peer) {
            peer.assigned = true;
            if (!peer.created) {
                peer.created = true;                // sole place created set to true
                creates.Add(peer.entity.id, peer);  // sole place a peer (entity) is added
                return true;
            }
            return false;
        }
        
        internal void AddUpdate (PeerEntity<T> peer) {
            peer.assigned = true;
            if (!peer.updated) {
                peer.updated = true;                // sole place created set to true
                updates.Add(peer.entity.id, peer);  // sole place a peer (entity) is added
            }
        }
        
        internal void AddDelete (string id) {
            deletes.Add(id);
        }
        
        // --- Read
        internal ReadTask<T> Read() {
            var read = new ReadTask<T>(set);
            reads.Add(read);
            return read;
        }
        
        // --- Query
        internal QueryTask<T> QueryFilter(FilterOperation filter) {
            var filterLinq = filter.Linq;
            if (queries.TryGetValue(filterLinq, out QueryTask<T> query))
                return query;
            query = new QueryTask<T>(filter);
            queries.Add(filterLinq, query);
            return query;
        }
        
        // --- Create
        internal CreateTask<T> Create(T entity) {
            var peer = set.CreatePeer(entity);
            AddCreate(peer);
            var create = new CreateTask<T>(new List<T>{peer.entity}, set);
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
            var update = new UpdateTask<T>(new List<T>{peer.entity}, set);
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
        internal PatchTask<T> Patch(PeerEntity<T> peer) {
            var patchTask  = new PatchTask<T>(peer, set);
            patchTasks.Add(patchTask);
            return patchTask;
        }
        
        internal PatchTask<T> PatchRange(ICollection<PeerEntity<T>> peers) {
            var patchTask  = new PatchTask<T>(peers, set);
            patchTasks.Add(patchTask);
            return patchTask;
        }
        
        // --- Delete
        internal DeleteTask<T> Delete(string id) {
            AddDelete(id);
            var delete = new DeleteTask<T>(new List<string>{id}, set);
            deleteTasks.Add(delete);
            return delete;
        }
        
        internal DeleteTask<T> DeleteRange(ICollection<string> ids) {
            foreach (var id in ids) {
                AddDelete(id);
            }
            var delete = new DeleteTask<T>(ids.ToList(), set);
            deleteTasks.Add(delete);
            return delete;
        }
        
        // --- Log changes -> create patches
        internal void LogSetChanges(Dictionary<string, PeerEntity<T>> peers, LogTask logTask) {
            foreach (var peerPair in peers) {
                PeerEntity<T> peer = peerPair.Value;
                GetEntityChanges(peer, logTask);
            }
        }

        internal void LogEntityChanges(T entity, LogTask logTask) {
            var peer = set.GetPeerById(entity.id);
            GetEntityChanges(peer, logTask);
        }

        /// In case the given entity was added via <see cref="Create"/> (peer.create != null) trace the entity to
        /// find changes in referenced entities in <see cref="Ref{T}"/> fields of the given entity.
        /// In these cases <see cref="RefMapper{T}.Trace"/> add untracked entities (== have no <see cref="PeerEntity{T}"/>)
        /// which is not already assigned) 
        private void GetEntityChanges(PeerEntity<T> peer, LogTask logTask) {
            if (peer.created) {
                set.intern.store._intern.tracerLogTask = logTask;
                set.intern.tracer.Trace(peer.entity);
                return;
            }
            var patchSource = peer.PatchSource;
            if (patchSource != null) {
                var diff = set.intern.objectPatcher.differ.GetDiff(patchSource, peer.entity);
                if (diff == null)
                    return;
                var patchList = set.intern.objectPatcher.CreatePatches(diff);
                var entityPatch = new EntityPatch {
                    patches = patchList
                };
                SetNextPatchSource(peer); // todo next patch source need to be set on Sync() 
                patches[peer.entity.id] = entityPatch;
                logTask.AddPatch(this, peer.entity.id);
            }
        }

        internal override void AddTasks(List<DatabaseTask> tasks) {
            // --- CreateEntities
            if (creates.Count > 0) {
                var entries = new Dictionary<string, EntityValue>();
                foreach (var createPair in creates) {
                    T entity = createPair.Value.entity;
                    var json = set.intern.jsonMapper.Write(entity);
                    var entry = new EntityValue(json);
                    entries.Add(entity.id, entry);
                }
                var req = new CreateEntities {
                    container = set.name,
                    entities = entries
                };
                tasks.Add(req);
            }
            // --- UpdateEntities
            if (updates.Count > 0) {
                var entries = new Dictionary<string, EntityValue>();
                foreach (var updatePair in updates) {
                    T entity = updatePair.Value.entity;
                    var json = set.intern.jsonMapper.Write(entity);
                    var entry = new EntityValue(json);
                    entries.Add(entity.id, entry);
                }
                var req = new UpdateEntities {
                    container = set.name,
                    entities = entries
                };
                tasks.Add(req);
            }
            // --- ReadEntities
            if (reads.Count > 0) {
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
                    var req = new ReadEntities {
                        ids = read.idMap.Keys.ToHashSet(),
                        references = references
                    };
                    readList.reads.Add(req);
                }
                tasks.Add(readList);
            }
            // --- QueryEntities
            if (queries.Count > 0) {
                foreach (var queryPair in queries) {
                    QueryTask<T> query = queryPair.Value;
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
            // --- PatchEntities
            foreach (var patchTask in patchTasks) {
                var memberAccess    = new MemberAccess(patchTask.members);
                var memberAccessor  = new MemberAccessor(set.intern.store._intern.jsonMapper.writer);
                
                foreach (var peer in patchTask.peers) {
                    var entityPatch     = AddEntityPatch(peer);
                    var selectResults   = memberAccessor.GetValues(peer.entity, memberAccess);
                    int n = 0;
                    foreach (var path in patchTask.members) {
                        var value = new JsonValue {
                            json = selectResults[n++].Json
                        };
                        entityPatch.Add(new PatchReplace {
                            path = path,
                            value = value
                        });
                    }
                }
            }
            if (patches.Count > 0) {
                var req = new PatchEntities {
                    container = set.name,
                    patches = new Dictionary<string, EntityPatch>(patches)
                };
                tasks.Add(req);
            }
            // --- DeleteEntities
            if (deletes.Count > 0) {
                var req = new DeleteEntities {
                    container   = set.name,
                    ids         = new HashSet<string>(deletes)
                };
                tasks.Add(req);
                deletes.Clear();
            }
        }

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
        
        private void SetNextPatchSource(PeerEntity<T> peer) {
            var json = set.intern.jsonMapper.writer.Write(peer.entity);
            peer.SetNextPatchSource(set.intern.jsonMapper.Read<T>(json));
        }

        private List<JsonPatch> AddEntityPatch(PeerEntity<T> peer) {
            var id = peer.entity.id;
            if (!patches.TryGetValue(id, out EntityPatch patch)) {
                patch = new EntityPatch {
                    patches = new List<JsonPatch>()
                };
                patches.Add(id, patch);
                SetNextPatchSource(peer);
            }
            return patch.patches;
        }
        
        private static int Some(int count) { return count != 0 ? 1 : 0; }

        internal void SetTaskInfo(ref SetInfo info) {
            info.tasks =
                Some(reads.Count)   +
                queries.Count       +
                Some(creates.Count) +
                Some(updates.Count) +
                Some(patches.Count + patchTasks.Count) +
                Some(deletes.Count);
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