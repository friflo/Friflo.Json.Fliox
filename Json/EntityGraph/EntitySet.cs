// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Flow.Graph.Query;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.EntityGraph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  abstract  Type            Type { get;  }
        
        internal  abstract  void            AddCommands           (List<DatabaseCommand> commands);
        //          
        internal  abstract  void            CreateEntitiesResult  (CreateEntities command, CreateEntitiesResult result);
        internal  abstract  void            ReadEntitiesResult    (ReadEntities   command, ReadEntitiesResult   result);
        internal  abstract  void            ReadReferenceResult   (ReadReference  command, ReadReferenceResult result, List<string> parentIds, ReadRefMap map);
        internal  abstract  void            PatchEntitiesResult   (PatchEntities  command, PatchEntitiesResult  result);

        public    abstract  int             LogSetChanges();
        internal  abstract  void            SyncReferences        (ContainerEntities containerResults);

    }
    
    public class EntitySet<T> : EntitySet where T : Entity
    {
        public  readonly    Type                                type;
        private readonly    EntityStore                         store;
        private readonly    TypeMapper<T>                       typeMapper;
        private readonly    ObjectMapper                        jsonMapper;
        private readonly    EntityContainer                     container;
        private readonly    ObjectPatcher                       objectPatcher;
        private readonly    Tracer                              tracer;
        
        private readonly    Dictionary<string, PeerEntity<T>>   peers       = new Dictionary<string, PeerEntity<T>>();
        private readonly    Dictionary<string, Read<T>>         reads       = new Dictionary<string, Read<T>>();
        private readonly    Dictionary<string, Create<T>>       creates     = new Dictionary<string, Create<T>>();
        private readonly    Dictionary<string, EntityPatch>     patches     = new Dictionary<string, EntityPatch>();
        
        private             Dictionary<string, ReadRefMap>      readRefMap  = new Dictionary<string, ReadRefMap>();
        private             Dictionary<string, ReadRefMap>      syncReadRefMap;


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
        }

        internal ReadRefMap GetReadRefMap<TValue>(string selector) {
            if (readRefMap.TryGetValue(selector, out ReadRefMap result))
                return result;
            result = new ReadRefMap(selector, typeof(TValue));
            readRefMap.Add(selector, result);
            return result;
        }
        
        private PeerEntity<T> CreatePeer (T entity) {
            if (peers.TryGetValue(entity.id, out PeerEntity<T> peer)) {
                if (peer.entity != entity)
                    throw new InvalidOperationException("");
                return peer;
            }
            peer = new PeerEntity<T>(entity);
            peers.Add(entity.id, peer);
            return peer;
        }
        
        internal Create<T> AddCreate (PeerEntity<T> peer) {
            peer.assigned = true;
            var create = peer.create;
            if (create == null) {
                peer.create = create = new Create<T>(peer.entity, store);
            }
            creates.Add(peer.entity.id, create);
            return create;
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
        
        public Read<T> Read(string id) {
            if (reads.TryGetValue(id, out Read<T> read))
                return read;
            var peer = GetPeerById(id);
            read = peer.read;
            if (read == null) {
                peer.read = read = new Read<T>(peer.entity.id, this);
            }
            reads.Add(id, read);
            return read;
        }

        // lab interface
        public ReadWhere<T> ReadWhere(Expression<Func<T, bool>> filter) {
            var op = Operation.FromFilter(filter);
            return default;
        }
        
        public Create<T> Create(T entity) {
            if (creates.TryGetValue(entity.id, out Create<T> create))
                return create;
            var peer = CreatePeer(entity);
            create = AddCreate(peer);
            return create;
        }

        public override int LogSetChanges() {
            foreach (var peerPair in peers) {
                PeerEntity<T> peer = peerPair.Value;
                GetEntityChanges(peer);
            }
            return creates.Count + patches.Values.Count;
        }
        
        private void GetEntityChanges(PeerEntity<T> peer) {
            if (peer.create != null) {
                tracer.Trace(peer.entity);
                return;
            }
            if (peer.patchReference != null) {
                var diff = objectPatcher.differ.GetDiff(peer.patchReference, peer.entity);
                if (diff == null)
                    return;
                var patchList = objectPatcher.CreatePatches(diff);
                var id = peer.entity.id;
                var entityPatch = new EntityPatch {
                    id = id,
                    patches = patchList
                };
                var json = jsonMapper.writer.Write(peer.entity);
                peer.nextPatchReference = jsonMapper.Read<T>(json);
                patches[peer.entity.id] = entityPatch;
            }
        }

        public int LogEntityChanges(T entity) {
            var peer = GetPeerById(entity.id);
            GetEntityChanges(peer);
            var patch = patches[entity.id];
            return patch.patches.Count;
        }

        internal override void AddCommands(List<DatabaseCommand> commands) {
            // --- CreateEntities
            if (creates.Count > 0) {
                var entries = new Dictionary<string, EntityValue>();
                foreach (var createPair in creates) {
                    Create<T> create = createPair.Value;
                    var entity = create.Entity;
                    var json = jsonMapper.Write(entity);
                    var entry = new EntityValue(json);
                    entries.Add(entity.id, entry);
                }
                var req = new CreateEntities {
                    container = container.name,
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
                    ReadRefMap map = refPair.Value;
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
                    container = container.name,
                    ids = ids,
                    references = references
                };
                commands.Add(req);
                syncReadRefMap = readRefMap;
                readRefMap = new Dictionary<string, ReadRefMap>();
                reads.Clear();
            }
            // --- PatchEntities
            if (patches.Count > 0) {
                var req = new PatchEntities {
                    container = container.name,
                    entityPatches = patches.Values.ToList()
                };
                commands.Add(req);
                patches.Clear();
            }
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
            for (int n = 0; n < result.references.Count; n++) {
                ReadReference          reference = command.references[n];
                ReadReferenceResult    refResult  = result.references[n];
                var refContainer = store.intern.setByName[refResult.container];
                ReadRefMap map = syncReadRefMap[reference.refPath];
                refContainer.ReadReferenceResult(reference, refResult, command.ids, map);
            }
            syncReadRefMap = null;
        }

        internal override void ReadReferenceResult(ReadReference command, ReadReferenceResult result, List<string> parentIds, ReadRefMap map) {
            foreach (var parentId in parentIds) {
                var reference = map.readRefs[parentId];
                if (reference.singleResult) {
                    var singleRef = (ReadRef<T>) reference;
                    if (result.ids.Count != 1)
                        throw new InvalidOperationException("Expect exactly one reference");
                    var id = result.ids[0];
                    var peer = GetPeerById(id);
                    singleRef.id        = id;
                    singleRef.entity    = peer.entity;
                    singleRef.synced    = true;
                } else {
                    var multiRef = (ReadRefs<T>) reference;
                    multiRef.synced = true;
                    for (int o = 0; o < result.ids.Count; o++) {
                        var id = result.ids[o];
                        var peer = GetPeerById(id);
                        var readRef = new ReadRef<T>(reference.parentId, reference.parentSet, reference.label) {
                            id      = id,
                            entity  = peer.entity,
                            synced  = true
                        };
                        multiRef.results.Add(readRef);
                    }
                }
            }
        }

        internal override void SyncReferences(ContainerEntities containerResults) {
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