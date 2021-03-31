// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Diff;
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
        internal  abstract  void            PatchEntitiesResult   (PatchEntities  command, PatchEntitiesResult  result);

        internal  abstract  PatchEntities   CreatePatchesFromChanges();
    }
    
    public class EntitySet<T> : EntitySet where T : Entity
    {
        public  readonly    Type                                type;
        private readonly    EntityStore                         store;
        private readonly    TypeMapper<T>                       typeMapper;
        private readonly    JsonMapper                          jsonMapper;
        private readonly    EntityContainer                     container;
        private readonly    Dictionary<string, PeerEntity<T>>   peers       = new Dictionary<string, PeerEntity<T>>();
        private readonly    Dictionary<string, Read<T>>         reads       = new Dictionary<string, Read<T>>();
        private readonly    Dictionary<string, Create<T>>       creates     = new Dictionary<string, Create<T>>();
        private readonly    Dictionary<string, EntityPatch>     patches     = new Dictionary<string, EntityPatch>();
        private readonly    ObjectPatcher                       objectPatcher;
            
        
        public EntitySet(EntityStore store) {
            this.store = store;
            type = typeof(T);
            store.intern.setByType[type]       = this;
            store.intern.setByName[type.Name]  = this;
            
            jsonMapper = store.intern.jsonMapper;
            typeMapper = (TypeMapper<T>)store.intern.typeStore.GetTypeMapper(typeof(T));
            container = store.intern.database.GetContainer(type.Name);
            objectPatcher = store.intern.objectPatcher;
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
        
        internal Create<T> AddCreate (PeerEntity<T> peer) {
            peer.assigned = true;
            var create = peer.create;
            if (create == null) {
                peer.create = create = new Create<T>(peer.entity, store);
            }
            creates.Add(peer.entity.id, create);
            return create;
        }
        
        internal PeerEntity<T> GetPeer(Ref<T> reference) {
            string id = reference.Id;
            PeerEntity<T> peer = reference.peer;
            if (peer == null) {
                var entity = reference.GetEntity();
                if (entity != null)
                    peer = CreatePeer(entity);
                else
                    peer = GetPeer(id);
            }
            return peer;
        }

        internal PeerEntity<T> GetPeer(string id) {
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
            var peer = GetPeer(id);
            read = peer.read;
            if (read == null) {
                peer.read = read = new Read<T>(peer.entity.id);
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

        public int SaveChanges() {
            var patchEntities = CreatePatchesFromChanges();
            return patchEntities.entityPatches.Count;
        }

        internal override PatchEntities CreatePatchesFromChanges() {
            var entityPatches = new List<EntityPatch>(); 
            var patchEntities = new PatchEntities {
                container = container.name,
                entityPatches = entityPatches
            };
            foreach (var peerPair in peers) {
                PeerEntity<T> peer = peerPair.Value;
                if (peer.create != null) {
                    var tracer = new Tracer(store.intern.typeCache, store);
                    tracer.Trace(peer.entity);
                    continue;
                }
                if (peer.patchReference != null) {
                    var diff = objectPatcher.differ.GetDiff(peer.patchReference, peer.entity);
                    if (diff == null)
                        continue;
                    var patchList = objectPatcher.CreatePatches(diff);
                    var id = peer.entity.id;
                    var entityPatch = new EntityPatch {
                        id = id,
                        patches = patchList
                    };
                    var json = jsonMapper.writer.Write(peer.entity);
                    peer.nextPatchReference = jsonMapper.Read<T>(json);
                    entityPatches.Add(entityPatch);
                    patches[peer.entity.id] = entityPatch;
                }
            }
            return patchEntities;
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
                var req = new ReadEntities {
                    container = container.name,
                    ids = ids
                };
                commands.Add(req);
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
                var peer = GetPeer(entry.key);
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
                
                var peer = GetPeer(entry.key);
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
        }
        
        // --- ReadEntities
        internal override void PatchEntitiesResult(PatchEntities command, PatchEntitiesResult result) {
            var entityPatches = command.entityPatches;
            foreach (var entityPatch in entityPatches) {
                var id = entityPatch.id;
                var peer = GetPeer(id);
                peer.patchReference = peer.nextPatchReference;
                peer.nextPatchReference = null;
            }
        }
    }
}