// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Models;
using Friflo.Json.Flow.Graph.Internal.Map;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal abstract class SyncSet
    {
        internal  abstract  void    AddTasks                (List<DatabaseTask> tasks);
        
        internal  abstract  void    CreateEntitiesResult    (CreateEntities task, CreateEntitiesResult result);
        internal  abstract void     ReadEntitiesResult      (ReadEntities   task, ReadEntitiesResult   result, ContainerEntities entities);
        internal  abstract  void    QueryEntitiesResult     (QueryEntities  task, QueryEntitiesResult  result);
        internal  abstract  void    PatchEntitiesResult     (PatchEntities  task, PatchEntitiesResult  result);
        internal  abstract  void    DeleteEntitiesResult    (DeleteEntities task, DeleteEntitiesResult result);
    }


    /// Multiple instances of this class can be created when calling EntitySet.Sync() without awaiting the result.
    /// Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal class SyncSet<T> : SyncSet where T : Entity
    {
        // Note!
        // All fields must be private by all means to ensure that all scheduled tasks of a Sync() request managed
        // by this instance can be mapped to their task results safely.
        
        private readonly    EntitySet<T>                        set;
            
        private readonly    List<ReadTask<T>>                   reads        = new List<ReadTask<T>>();
        /// key: <see cref="QueryTask{T}.filterLinq"/> 
        private readonly    Dictionary<string, QueryTask<T>>    queries      = new Dictionary<string, QueryTask<T>>();
        
        /// key: <see cref="PeerEntity{T}.entity"/>.id
        private readonly    Dictionary<string, PeerEntity<T>>   creates      = new Dictionary<string, PeerEntity<T>>();
        private readonly    List<CreateTask>                    createTasks  = new List<CreateTask>();

        /// key: <see cref="EntityPatch.id"/>
        private readonly    Dictionary<string, EntityPatch>     patches      = new Dictionary<string, EntityPatch>();
        /// key: entity id
        private readonly    HashSet<string>                     deletes      = new HashSet   <string>();
        private readonly    List<DeleteTask>                    deleteTasks  = new List<DeleteTask>();

        internal SyncSet(EntitySet<T> set) {
            this.set = set;
        }
        
        internal void AddCreate (PeerEntity<T> peer) {
            peer.assigned = true;
            if (!peer.created) {
                peer.created = true;                // sole place created set to true
                creates.Add(peer.entity.id, peer);  // sole place a peer (entity) is added
            }
        }
        
        internal ReadTask<T> Read() {
            var read = new ReadTask<T>(set);
            reads.Add(read);
            return read;
        }
        
        internal QueryTask<T> QueryFilter(FilterOperation filter) {
            var filterLinq = filter.Linq;
            if (queries.TryGetValue(filterLinq, out QueryTask<T> query))
                return query;
            query = new QueryTask<T>(filter);
            queries.Add(filterLinq, query);
            return query;
        }
        
        internal CreateTask<T> Create(T entity) {
            var peer = set.CreatePeer(entity);
            AddCreate(peer);
            var create = new CreateTask<T>(peer.entity);
            createTasks.Add(create);
            return create;
        }
        
        internal CreateRangeTask<T> CreateRange(ICollection<T> entities) {
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                AddCreate(peer);
            }
            var create = new CreateRangeTask<T>(entities);
            createTasks.Add(create);
            return create;
        }
        
        internal DeleteTask<T> Delete(string id) {
            deletes.Add(id);
            var delete = new DeleteTask<T>(id);
            deleteTasks.Add(delete);
            return delete;
        }
        
        internal DeleteRangeTask<T> DeleteRange(ICollection<string> ids) {
            foreach (var id in ids) {
                deletes.Add(id);
            }
            var delete = new DeleteRangeTask<T>(ids);
            deleteTasks.Add(delete);
            return delete;
        }
        
        internal int LogSetChanges(Dictionary<string, PeerEntity<T>> peers) {
            foreach (var peerPair in peers) {
                PeerEntity<T> peer = peerPair.Value;
                GetEntityChanges(peer);
            }
            return creates.Count + patches.Values.Count;
        }

        internal int LogEntityChanges(T entity) {
            var peer = set.GetPeerById(entity.id);
            var patch = GetEntityChanges(peer);
            if (patch != null)
                return patch.patches.Count;
            return 0;
        }

        /// In case the given entity was added via <see cref="Create"/> (peer.create != null) trace the entity to
        /// find changes in referenced entities in <see cref="Ref{T}"/> fields of the given entity.
        /// In these cases <see cref="RefMapper{T}.Trace"/> add untracked entities (== have no <see cref="PeerEntity{T}"/>)
        /// which is not already assigned) 
        private EntityPatch GetEntityChanges(PeerEntity<T> peer) {
            if (peer.created) {
                set.intern.tracer.Trace(peer.entity);
                return null;
            }
            var patchSource = peer.PatchSource;
            if (patchSource != null) {
                var diff = set.intern.objectPatcher.differ.GetDiff(patchSource, peer.entity);
                if (diff == null)
                    return null;
                var patchList = set.intern.objectPatcher.CreatePatches(diff);
                var entityPatch = new EntityPatch {
                    patches = patchList
                };
                var json = set.intern.jsonMapper.writer.Write(peer.entity);
                peer.SetNextPatchSource(set.intern.jsonMapper.Read<T>(json));
                patches[peer.entity.id] = entityPatch;
                return entityPatch;
            }
            return null;
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
                creates.Clear();
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
            if (patches.Count > 0) {
                var req = new PatchEntities {
                    container = set.name,
                    patches = new Dictionary<string, EntityPatch>(patches)
                };
                tasks.Add(req);
                patches.Clear();
            }
            // --- DeleteEntities
            if (deletes.Count > 0) {
                var req = new DeleteEntities {
                    container = set.name,
                    ids = new List<string>()
                };
                foreach (var id in deletes) {
                    req.ids.Add(id);
                }
                tasks.Add(req);
                deletes.Clear();
            }
        }

        private void AddReferences(List<References> references, SubRefs refs) {
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
        
        internal override void CreateEntitiesResult(CreateEntities task, CreateEntitiesResult result) {
            var entities = task.entities;
            foreach (var entry in entities) {
                var peer = set.GetPeerById(entry.Key);
                peer.created = false;
                peer.SetPatchSource(set.intern.jsonMapper.Read<T>(entry.Value.value.json));
            }
            foreach (var createTask in createTasks) {
                createTask.synced = true;
            }
        }
        
        internal override void ReadEntitiesResult(ReadEntities task, ReadEntitiesResult result, ContainerEntities entities) {
            // remove all requested peers from EntitySet which are not present in database
            foreach (var id in task.ids) {
                var value = entities.entities[id];
                var json = value.value.json;  // in case of RemoteClient json is "null"
                var isNull = json == null || json == "null";
                if (isNull)
                    set.DeletePeer(id);
            }
            // todo check for optimization
            foreach (var read in reads) {
                var readIds = read.idMap.Keys.ToList();
                foreach (var id in readIds) {
                    var value = entities.entities[id];
                    var json = value.value.json;  // in case of RemoteClient json is "null"
                    if (json == null || json == "null") {
                        read.idMap[id] = null;
                    } else {
                        var peer = set.GetPeerById(id);
                        read.idMap[id] = peer.entity;
                    }
                }
                read.synced = true;
                AddReferencesResult(task.references, result.references, read.refsTask.subRefs);
            }
        }
        
        internal override void QueryEntitiesResult(QueryEntities task, QueryEntitiesResult result) {
            var filterLinq = result.filterLinq;
            var query = queries[filterLinq];
            var entities = query.entities = new Dictionary<string, T>(result.ids.Count);
            foreach (var id in result.ids) {
                var peer = set.GetPeerById(id);
                entities.Add(id, peer.entity);
            }
            AddReferencesResult(task.references, result.references, query.refsTask.subRefs);
            query.synced = true;
        }

        private void AddReferencesResult(List<References> references, List<ReferencesResult> referencesResult, SubRefs refs) {
            // in case (references != null &&  referencesResult == null) => no reference ids found for references 
            if (references == null || referencesResult == null)
                return;
            for (int n = 0; n < references.Count; n++) {
                References          reference    = references[n];
                ReferencesResult    refResult    = referencesResult[n];
                EntitySet           refContainer = set.intern.store._intern.setByName[refResult.container];
                ReadRefsTask        subRef       = refs[reference.selector];
                subRef.SetResult(refContainer, refResult.ids);

                var subReferences = reference.references;
                if (subReferences != null) {
                    var readRefs = subRef.SubRefs;
                    AddReferencesResult(subReferences, refResult.references, readRefs);
                }
            }
        }
        
        internal override void PatchEntitiesResult(PatchEntities task, PatchEntitiesResult result) {
            var entityPatches = task.patches;
            foreach (var entityPatchPair in entityPatches) {
                var id = entityPatchPair.Key;
                var peer = set.GetPeerById(id);
                peer.SetPatchSource(peer.NextPatchSource);
                peer.SetNextPatchSourceNull();
            }
        }

        internal override void DeleteEntitiesResult(DeleteEntities task, DeleteEntitiesResult result) {
            foreach (var id in task.ids) {
                set.DeletePeer(id);
            }
            foreach (var deleteTask in deleteTasks) {
                deleteTask.synced = true;
            }
        }

        private static int Some(int count) { return count != 0 ? 1 : 0; }

        internal void SetTaskInfo(ref SetInfo info) {
            info.tasks =
                reads.Count         +
                queries.Count       +
                Some(creates.Count) +
                Some(patches.Count) +
                Some(deletes.Count);
            //
            info.reads      = reads.Count;
            info.queries    = queries.Count;
            info.create     = creates.Count;
            info.patch      = patches.Count;
            info.delete     = deletes.Count;
            // info.readRefs   = readRefsMap.Count;
        }
    }
}