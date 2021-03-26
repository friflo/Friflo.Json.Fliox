// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        public  abstract    void    AddCommands           (List<DatabaseCommand> commands);
        //
        public  abstract    void    CreateEntitiesResult  (CreateEntities command, CreateEntitiesResult result);
        public  abstract    void    ReadEntitiesResult    (ReadEntities   command, ReadEntitiesResult   result);
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
            
        
        public EntitySet(EntityStore store) {
            this.store = store;
            type = typeof(T);
            store.setByType[type]       = this;
            store.setByName[type.Name]  = this;
            
            jsonMapper = store.jsonMapper;
            typeMapper = (TypeMapper<T>)store.typeStore.GetTypeMapper(typeof(T));
            container = store.database.GetContainer(type.Name);
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
            if (peers.TryGetValue(id, out PeerEntity<T> peer))
                return peer;
            var entity = (T)typeMapper.CreateInstance();
            peer = new PeerEntity<T>(entity);
            peer.entity.id = id;
            peers.Add(id, peer);
            return peer;
        }
        
        internal void SetRefPeer(Ref<T> reference) {
            if (reference.peer != null) {
                return;
            }
            var peer = GetPeer(reference);
            if (!peer.assigned)
                throw new PeerNotAssignedException(peer.entity);
            reference.peer = peer;
            reference.Entity = peer.entity;
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

        public override void AddCommands(List<DatabaseCommand> commands) {
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
                    containerName = container.name,
                    entities = entries
                };
                commands.Add(req);
                creates.Clear();
            }
            // --- ReadEntities
            if (reads.Count > 0) {
                var ids = reads.Select(read => read.Key).ToList();
                var req = new ReadEntities {
                    containerName = container.name,
                    ids = ids
                };
                commands.Add(req);
                reads.Clear();
            }
        }

        // --- CreateEntities
        public override void CreateEntitiesResult(CreateEntities command, CreateEntitiesResult result) {
            var entities = command.entities;
            foreach (var entry in entities) {
                var peer = GetPeer(entry.key);
                peer.create = null;
            }
        }
        
        // --- ReadEntities
        public override void ReadEntitiesResult(ReadEntities command, ReadEntitiesResult result) {
            var entries = result.entities;
            if (entries.Count != command.ids.Count)
                throw new InvalidOperationException($"Expect returning same number of entities {entries.Count} as number ids {command.ids.Count}");
                
            foreach (var entry in entries) {
                var peer = GetPeer(entry.key);
                var read = peer.read;
                if (entry.value.json != null) {
                    jsonMapper.ReadTo(entry.value.json, peer.entity);
                    peer.assigned = true;
                    if (read != null) {
                        read.result = peer.entity;
                        read.synced = true;
                    }
                } else {
                    if (read != null) {
                        read.result = null;
                        read.synced = true;
                    }
                }
                peer.read = null;
            }
        }
    }
}