// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Graph;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  abstract  void            AddCommands           (List<DatabaseCommand> commands);
        //          
        internal  abstract  void            CreateEntitiesResult  (CreateEntities command, CreateEntitiesResult result);
        internal  abstract  void            ReadEntitiesResult    (ReadEntities   command, ReadEntitiesResult   result);
        internal  abstract  void            ReadDependencyResult  (ReadDependency command, ReadDependencyResult result);
        internal  abstract  void            PatchEntitiesResult   (PatchEntities  command, PatchEntitiesResult  result);

        public    abstract  int             LogSetChanges();
    }
    
    public class EntitySet<T> : EntitySet where T : Entity
    {
        public  readonly    Type                                type;
        private readonly    EntityStore                         store;
        private readonly    TypeMapper<T>                       typeMapper;
        private readonly    JsonMapper                          jsonMapper;
        private readonly    EntityContainer                     container;
        private readonly    ObjectPatcher                       objectPatcher;
        private readonly    Tracer                              tracer;
        
        private readonly    Dictionary<string, PeerEntity<T>>   peers       = new Dictionary<string, PeerEntity<T>>();
        private readonly    Dictionary<string, Read<T>>         reads       = new Dictionary<string, Read<T>>();
        private readonly    Dictionary<string, Create<T>>       creates     = new Dictionary<string, Create<T>>();
        private readonly    Dictionary<string, EntityPatch>     patches     = new Dictionary<string, EntityPatch>();
        
        internal readonly   Dictionary<string, ReadDeps>        readDeps    = new Dictionary<string, ReadDeps>();

        
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
                var entries = new List<KeyValue>();
                foreach (var createPair in creates) {
                    Create<T> create = createPair.Value;
                    var entity = create.Entity;
                    var json = jsonMapper.Write(entity);
                    var entry = new KeyValue {
                        key = entity.id,
                        value = new JsonValue{json = json }
                    };
                    entries.Add(entry);
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

                var dependencies = new List<ReadDependency>();
                foreach (var depPair in readDeps) {
                    ReadDeps depsById = depPair.Value;
                    ReadDependency readDep = new ReadDependency {
                        refPath = depsById.selector,
                        container = depsById.entityType.Name,
                        ids = new List<string>() 
                    };
                    foreach (IDependency dep in depsById.dependencies) {
                        readDep.ids.Add(dep.Id);
                    }
                    dependencies.Add(readDep);
                }
                var req = new ReadEntities {
                    container = container.name,
                    ids = ids,
                    dependencies = dependencies
                };
                commands.Add(req);
                readDeps.Clear();
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
                var peer = GetPeerById(entry.key);
                peer.create = null;
                peer.patchReference = jsonMapper.Read<T>(entry.value.json);
            }
        }
        
        // --- ReadEntities
        internal override void ReadEntitiesResult(ReadEntities command, ReadEntitiesResult result) {
            var entries = result.entities;
            if (entries.Count != command.ids.Count)
                throw new InvalidOperationException($"read command: Expect entities.Count of response matches request. expect: {command.ids.Count} got: {entries.Count}");
                
            for (int n = 0; n < entries.Count; n++) {
                var entry = entries[n];
                var expectedId = command.ids[n];
                if (entry.key != expectedId)
                    throw new InvalidOperationException($"read command: Expect entity key of response matches request: index:{n} expect: {expectedId} got: {entry.key}");
                
                var peer = GetPeerById(entry.key);
                var read = peer.read;
                var json = entry.value.json;
                if (json != null && "null" != json) {
                    jsonMapper.ReadTo(entry.value.json, peer.entity);
                    peer.patchReference = jsonMapper.Read<T>(entry.value.json);
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
            for (int n = 0; n < result.dependencies.Count; n++) {
                ReadDependencyResult dependency = result.dependencies[n];
                var depContainer = store.intern.setByName[dependency.container];
                depContainer.ReadDependencyResult(null, dependency);
            }
        }

        internal override void ReadDependencyResult(ReadDependency command, ReadDependencyResult result) {
            for (int o = 0; o < result.entities.Count; o++) {
                var keyValue = result.entities[o];
                var peer = GetPeerById(keyValue.key);
                jsonMapper.ReadTo(keyValue.value.json, peer.entity);
                peer.assigned = true;
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