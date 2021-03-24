// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.EntityGraph
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        public  abstract    void    CreateEntitiesResponse      (CreateEntitiesRequest create);
        public  abstract    void    ReadEntitiesResponse        (ReadEntitiesRequest read);
    }
    
    public class EntitySet<T> : EntitySet where T : Entity
    {
        public  readonly    Type                                type;
        private readonly    EntityStore                         store;
        private readonly    TypeMapper<T>                       typeMapper;
        private readonly    JsonMapper                          jsonMapper;
        private readonly    EntityContainer                     container;
        private readonly    Dictionary<string, PeerEntity<T>>   peers       = new Dictionary<string, PeerEntity<T>>();  // todo -> HashSet<>
            
        public              int                                 Count       => peers.Count;
        
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
        
        internal void AddCreateRequest (PeerEntity<T> peer) {
            peer.assigned = true;
            CreateEntityRequest(null, peer.entity);
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
            var read = new Read<T>(id);
            ReadEntityRequest(read);
            return read;
        }
        
        public Create<T> Create(T entity) {
            var create = new Create<T>(entity, store);
            var peer = CreatePeer(entity);
            AddCreateRequest(peer);
            return create;
        }


        // --- CreateEntities request / result ---
        private void CreateEntityRequest(Create<T> create, T entity) {
            var req = new CreateEntitiesRequest {
                containerName = container.name,
            };
            var json = jsonMapper.Write(entity);
            var entry = new KeyValue {
                key = entity.id,
                value = json
            };
            List<KeyValue> entries = new List<KeyValue> {entry};
            req.entities = entries;
            store.AddRequest(req);
        }

        public override void CreateEntitiesResponse(CreateEntitiesRequest create) {
            // may handle success/error of entity creation
        }
        
        // --- ReadEntities request / result ---
        private void ReadEntityRequest(Read<T> read) {
            var req = new ReadEntitiesRequest {
                containerName = container.name,
                command = read
            };
            List<string> ids = new List<string> {read.id};
            req.ids = ids;
            store.AddRequest(req);
        }

        public override void ReadEntitiesResponse(ReadEntitiesRequest readRequest) {
            var entries = readRequest.entitiesResult;
            if (entries.Count != readRequest.ids.Count)
                throw new InvalidOperationException($"Expect returning same number of entities {entries.Count} as number ids {readRequest.ids.Count}");
                
            foreach (var entry in entries) {
                if (entry.value != null) {
                    var peer = GetPeer(entry.key);
                    jsonMapper.ReadTo(entry.value, peer.entity);
                    peer.assigned = true;
                    var read = (Read<T>)readRequest.command;
                    read.result = peer.entity;
                    read.synced = true;
                } else {
                    var read = (Read<T>)readRequest.command;
                    read.result = null;
                    read.synced = true;
                }
            }
        }
    }
}