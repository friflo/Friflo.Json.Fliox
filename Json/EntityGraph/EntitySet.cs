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
        public  abstract    void    CreateStoreRequest  (StoreSyncRequest syncRequest);
        //
        public  abstract    void    CreateEntities      (CreateEntitiesRequest create);
        public  abstract    void    ReadEntities        (ReadEntitiesRequest read);
    }
    
    public class EntitySet<T> : EntitySet where T : Entity
    {
        public  readonly    Type                                type;
        private readonly    EntityStore                         store;
        private readonly    TypeMapper<T>                       typeMapper;
        private readonly    JsonMapper                          jsonMapper;
        private readonly    EntityContainer                     container;
        private readonly    Dictionary<string, PeerEntity<T>>   peers       = new Dictionary<string, PeerEntity<T>>();  // todo -> HashSet<>
        private readonly    List<Read<T>>                       reads       = new List<Read<T>>();  // todo -> HashSet<>
        private readonly    List<T>                             creates     = new List<T>();  // todo -> HashSet<>
            
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
            creates.Add(peer.entity);
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
            reads.Add(read);
            return read;
        }
        
        public Create<T> Create(T entity) {
            var create = new Create<T>(entity, store);
            var peer = CreatePeer(entity);
            AddCreateRequest(peer);
            return create;
        }

        public override void CreateStoreRequest(StoreSyncRequest syncRequest) {
            // creates
            if (creates.Count > 0) {
                var req = new CreateEntitiesRequest { containerName = container.name };
                syncRequest.requests.Add(req);
                List<KeyValue> entries = new List<KeyValue>();
                foreach (var entity in creates) {
                    var entry = new KeyValue {
                        key = entity.id,
                        value = jsonMapper.Write(entity)
                    };
                    entries.Add(entry);
                }
                req.entities = entries;
                container.CreateEntities(entries);
                creates.Clear();
            }
            
            // reads
            if (reads.Count > 0) {
                var req = new ReadEntitiesRequest{ containerName = container.name };
                syncRequest.requests.Add(req);
                List<string> ids = new List<string>();
                reads.ForEach(read => ids.Add(read.id));
                req.ids = ids;
                
                var entries = container.ReadEntities(ids);
                if (entries.Count != reads.Count)
                    throw new InvalidOperationException($"Expect returning same number of entities {entries.Count} as number ids {ids.Count}");
                
                int n = 0;
                foreach (var entry in entries) {
                    if (entry.value != null) {
                        var peer = GetPeer(entry.key);
                        jsonMapper.ReadTo(entry.value, peer.entity);
                        peer.assigned = true;
                        var read = reads[n];
                        read.result = peer.entity;
                        read.synced = true;
                    } else {
                        var read = reads[n];
                        read.result = null;
                        read.synced = true;
                    }
                    n++;
                }
                reads.Clear();
            }
        }

        public override void CreateEntities(CreateEntitiesRequest create) {
            
        }

        public override void ReadEntities(ReadEntitiesRequest read) {
            
        }
    }
}