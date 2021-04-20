// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.EntityGraph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  abstract  Type            Type { get;  }
        
        internal  abstract  void            AddCommands           (List<DbCommand> commands);
        //          
        internal  abstract  void            CreateEntitiesResult  (CreateEntities command, CreateEntitiesResult result);
        internal  abstract  void            ReadEntitiesResult    (ReadEntities   command, ReadEntitiesResult   result);
        internal  abstract  void            ReadReferenceResult   (ReadReference  command, ReadReferenceResult  result, List<string> parentIds, ReadRefTaskMap map);
        internal  abstract  void            QueryEntitiesResult   (QueryEntities  command, QueryEntitiesResult  result);
        internal  abstract  void            PatchEntitiesResult   (PatchEntities  command, PatchEntitiesResult  result);

        public    abstract  int             LogSetChanges();
        internal  abstract  void            SyncEntities        (ContainerEntities containerResults);

    }
    
    public class EntitySet<T> : EntitySet where T : Entity
    {
        public   readonly   Type                                type;
        internal readonly   EntityStore                         store;
        private  readonly   TypeMapper<T>                       typeMapper;
        internal readonly   ObjectMapper                        jsonMapper;
        internal readonly   EntityContainer                     container;
        internal readonly   ObjectPatcher                       objectPatcher;
        internal readonly   Tracer                              tracer;
        
        /// key: <see cref="PeerEntity{T}.entity"/>.id
        private readonly    Dictionary<string, PeerEntity<T>>   peers       = new Dictionary<string, PeerEntity<T>>();

        internal readonly    EntitySetSync<T>                       sync;

        internal override   Type                                Type => type;
        
        public EntitySet(EntityStore store) {
            this.store = store;
            type = typeof(T);
            store.intern.setByType[type]       = this;
            store.intern.setByName[type.Name]  = this;
            
            jsonMapper = store.intern.jsonMapper;
            typeMapper = (TypeMapper<T>)store.intern.typeStore.GetTypeMapper(typeof(T));
            container = store.intern.database.GetContainer(type.Name);
            objectPatcher = store.intern.objectPatcher;
            tracer = new Tracer(store.intern.typeCache, store);

            sync = new EntitySetSync<T>(this);
        }

        internal PeerEntity<T> CreatePeer (T entity) {
            if (peers.TryGetValue(entity.id, out PeerEntity<T> peer)) {
                if (peer.entity != entity)
                    throw new InvalidOperationException("");
                return peer;
            }
            peer = new PeerEntity<T>(entity);
            peers.Add(entity.id, peer);
            return peer;
        }
        
        internal PeerEntity<T> GetPeerByRef(Ref<T> reference) {
            string id = reference.Id;
            PeerEntity<T> peer = reference.peer;
            if (peer == null) {
                var entity = reference.GetEntity();
                if (entity != null)
                    peer = CreatePeer(entity);
                else
                    peer = GetPeerById(id);
            }
            return peer;
        }

        internal PeerEntity<T> GetPeerById(string id) {
            if (peers.TryGetValue(id, out PeerEntity<T> peer)) {
                return peer;
            }
            var entity = (T)typeMapper.CreateInstance();
            peer = new PeerEntity<T>(entity);
            peer.entity.id = id;
            peers.Add(id, peer);
            return peer;
        }
        
        public ReadTask<T> Read(string id) {
            return sync.Read(id);
        }

        public QueryTask<T> Query(Expression<Func<T, bool>> filter) {
            var op = Operation.FromFilter(filter);
            return QueryFilter(op);
        }
        
        public QueryTask<T> QueryFilter(FilterOperation filter) {
            return sync.QueryFilter(filter);
        }
        
        public QueryTask<T> QueryAll() {
            var all = Operation.FilterTrue;
            return QueryFilter(all);
        }

        public CreateTask<T> Create(T entity) {
            return sync.Create(entity);
        }

        public override int LogSetChanges() {
            return sync.LogSetChanges(peers);
        }

        public int LogEntityChanges(T entity) {
            return sync.LogEntityChanges(entity);
        }

        internal override void AddCommands(List<DbCommand> commands) {
            sync.AddCommands(commands);
        }

        // --- CreateEntities
        internal override void CreateEntitiesResult(CreateEntities command, CreateEntitiesResult result) {
            var entities = command.entities;
            foreach (var entry in entities) {
                var peer = GetPeerById(entry.Key);
                peer.create = null;
                peer.patchReference = jsonMapper.Read<T>(entry.Value.value.json);
            }
        }
        
        // --- ReadEntities
        internal override void ReadEntitiesResult(ReadEntities command, ReadEntitiesResult result) {
            sync.ReadEntitiesResult(command, result);
        }

        internal override void ReadReferenceResult(ReadReference command, ReadReferenceResult result, List<string> parentIds, ReadRefTaskMap map) {
            foreach (var parentId in parentIds) {
                var reference = map.readRefs[parentId];
                if (reference.singleResult) {
                    var singleRef = (ReadRefTask<T>) reference;
                    if (result.ids.Count != 1)
                        throw new InvalidOperationException("Expect exactly one reference");
                    var id = result.ids[0];
                    var peer = GetPeerById(id);
                    singleRef.id        = id;
                    singleRef.entity    = peer.entity;
                    singleRef.synced    = true;
                } else {
                    var multiRef = (ReadRefsTask<T>) reference;
                    multiRef.synced = true;
                    for (int o = 0; o < result.ids.Count; o++) {
                        var id = result.ids[o];
                        var peer = GetPeerById(id);
                        var readRef = new ReadRefTask<T>(reference.parentId, reference.parentSet, reference.label) {
                            id      = id,
                            entity  = peer.entity,
                            synced  = true
                        };
                        multiRef.results.Add(readRef);
                    }
                }
            }
        }

        internal override void QueryEntitiesResult(QueryEntities command, QueryEntitiesResult result) {
            sync.QueryEntitiesResult(command, result);
        }

        internal override void SyncEntities(ContainerEntities containerResults) {
            foreach (var entity in containerResults.entities) {
                var id = entity.Key;
                var peer = GetPeerById(id);
                var read = peer.read;
                var json = entity.Value.value.json;
                if (json != null && "null" != json) {
                    jsonMapper.ReadTo(json, peer.entity);
                    peer.patchReference = jsonMapper.Read<T>(json);
                    peer.assigned = true;
                    if (read != null) {
                        read.result = peer.entity;
                        read.synced = true;
                    }
                } else {
                    peer.patchReference = null;
                    if (read != null) {
                        read.result = null;
                        read.synced = true;
                    }
                }
                peer.read = null;
            }
        }

        // --- ReadEntities
        internal override void PatchEntitiesResult(PatchEntities command, PatchEntitiesResult result) {
            var entityPatches = command.entityPatches;
            foreach (var entityPatch in entityPatches) {
                var id = entityPatch.id;
                var peer = GetPeerById(id);
                peer.patchReference = peer.nextPatchReference;
                peer.nextPatchReference = null;
            }
        }
    }
}