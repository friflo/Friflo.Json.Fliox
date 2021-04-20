// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.EntityGraph
{
    /// Multiple instances of this class can be created when calling EntitySet.Sync() without awaiting the result.
    /// Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal class EntitySetSync<T> where T : Entity
    {
        // Note!
        // All fields must be private to ensure that all scheduled tasks managed by this instance can be mapped
        // to their task results safely.
        
        private readonly    EntitySet<T>                        set;
            
        /// key: <see cref="ReadTask{T}.id"/>
        private readonly    Dictionary<string, ReadTask<T>>     reads       = new Dictionary<string, ReadTask<T>>();
        /// key: <see cref="QueryTask{T}.filter"/>.Linq 
        private             Dictionary<string, QueryTask<T>>    queries     = new Dictionary<string, QueryTask<T>>();   
        private             Dictionary<string, QueryTask<T>>    syncQueries;
        /// key: <see cref="CreateTask{T}.entity"/>.id
        private readonly    Dictionary<string, CreateTask<T>>   creates     = new Dictionary<string, CreateTask<T>>();
        /// key: <see cref="EntityPatch.id"/>
        private readonly    Dictionary<string, EntityPatch>     patches     = new Dictionary<string, EntityPatch>();
        /// key: <see cref="ReadRefTaskMap.selector"/>
        private             Dictionary<string, ReadRefTaskMap>  readRefMap  = new Dictionary<string, ReadRefTaskMap>();
        private             Dictionary<string, ReadRefTaskMap>  syncReadRefMap;

        internal EntitySetSync(EntitySet<T> set) {
            this.set = set;
        }
        
        internal ReadRefTaskMap GetReadRefMap<TValue>(string selector) {
            if (readRefMap.TryGetValue(selector, out ReadRefTaskMap result))
                return result;
            result = new ReadRefTaskMap(selector, typeof(TValue));
            readRefMap.Add(selector, result);
            return result;
        }
        
        internal CreateTask<T> AddCreate (PeerEntity<T> peer) {
            peer.assigned = true;
            var create = peer.create;
            if (create == null) {
                peer.create = create = new CreateTask<T>(peer.entity);
            }
            creates.Add(peer.entity.id, create);
            return create;
        }
        
        internal ReadTask<T> Read(string id) {
            if (reads.TryGetValue(id, out ReadTask<T> read))
                return read;
            var peer = set.GetPeerById(id);
            read = peer.read;
            if (read == null) {
                peer.read = read = new ReadTask<T>(peer.entity.id, set);
            }
            reads.Add(id, read);
            return read;
        }
        
        internal QueryTask<T> QueryFilter(FilterOperation filter) {
            var filterLinq = filter.Linq;
            if (queries.TryGetValue(filterLinq, out QueryTask<T> query))
                return query;
            query = new QueryTask<T>(filter, set);
            queries.Add(filterLinq, query);
            return query;
        }
        
        internal CreateTask<T> Create(T entity) {
            if (creates.TryGetValue(entity.id, out CreateTask<T> create))
                return create;
            var peer = set.CreatePeer(entity);
            create = AddCreate(peer);
            return create;
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
            GetEntityChanges(peer);
            var patch = patches[entity.id];
            return patch.patches.Count;
        }

        private void GetEntityChanges(PeerEntity<T> peer) {
            if (peer.create != null) {
                set.tracer.Trace(peer.entity);
                return;
            }
            if (peer.patchReference != null) {
                var diff = set.objectPatcher.differ.GetDiff(peer.patchReference, peer.entity);
                if (diff == null)
                    return;
                var patchList = set.objectPatcher.CreatePatches(diff);
                var id = peer.entity.id;
                var entityPatch = new EntityPatch {
                    id = id,
                    patches = patchList
                };
                var json = set.jsonMapper.writer.Write(peer.entity);
                peer.nextPatchReference = set.jsonMapper.Read<T>(json);
                patches[peer.entity.id] = entityPatch;
            }
        }

        internal void AddCommands(List<DbCommand> commands) {
            // --- CreateEntities
            if (creates.Count > 0) {
                var entries = new Dictionary<string, EntityValue>();
                foreach (var createPair in creates) {
                    CreateTask<T> create = createPair.Value;
                    var entity = create.Entity;
                    var json = set.jsonMapper.Write(entity);
                    var entry = new EntityValue(json);
                    entries.Add(entity.id, entry);
                }
                var req = new CreateEntities {
                    container = set.container.name,
                    entities = entries
                };
                commands.Add(req);
                creates.Clear();
            }
            // --- ReadEntities
            if (reads.Count > 0) {
                var ids = reads.Select(read => read.Key).ToList();

                var references = new List<ReadReference>();
                foreach (var refPair in readRefMap) {
                    ReadRefTaskMap map = refPair.Value;
                    ReadReference readReference = new ReadReference {
                        refPath = map.selector,
                        container = map.entityType.Name,
                        ids = new List<string>() 
                    };
                    foreach (var readRef in map.readRefs) {
                        readReference.ids.Add(readRef.Key);
                    }
                    references.Add(readReference);
                }
                var req = new ReadEntities {
                    container = set.container.name,
                    ids = ids,
                    references = references
                };
                commands.Add(req);
                syncReadRefMap = readRefMap;
                readRefMap = new Dictionary<string, ReadRefTaskMap>();
                reads.Clear();
            }
            // --- QueryEntities
            if (queries.Count > 0) {
                foreach (var queryPair in queries) {
                    var query = queryPair.Value;
                    var linq = query.filter.Linq;
                    var req = new QueryEntities {
                        container   = set.container.name,
                        filter      = query.filter,
                        filterLinq  = linq
                    };
                    commands.Add(req);
                }
                syncQueries = queries;
                queries = new Dictionary<string, QueryTask<T>>();
            }
            // --- PatchEntities
            if (patches.Count > 0) {
                var req = new PatchEntities {
                    container = set.container.name,
                    entityPatches = patches.Values.ToList()
                };
                commands.Add(req);
                patches.Clear();
            }
        }
        
        internal void ReadEntitiesResult(ReadEntities command, ReadEntitiesResult result) {
            for (int n = 0; n < result.references.Count; n++) {
                ReadReference          reference = command.references[n];
                ReadReferenceResult    refResult  = result.references[n];
                var refContainer = set.store.intern.setByName[refResult.container];
                ReadRefTaskMap map = syncReadRefMap[reference.refPath];
                refContainer.ReadReferenceResult(reference, refResult, command.ids, map);
            }
            syncReadRefMap = null;
        }
        
        internal void QueryEntitiesResult(QueryEntities command, QueryEntitiesResult result) {
            var filterLinq = result.filterLinq;
            var query = syncQueries[filterLinq];
            var entities = query.entities;
            foreach (var id in result.ids) {
                var peer = set.GetPeerById(id);
                entities.Add(peer.entity);
            }
            query.synced = true;
            syncQueries.Remove(filterLinq);
        }
    }
}