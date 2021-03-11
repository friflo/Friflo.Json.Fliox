// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Mapper.ER.Database;
using Friflo.Json.Mapper.ER.Map;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.ER
{
    // --------------------------------------- EntityStore ---------------------------------------
    public class EntityStore : IDisposable
    {
        internal readonly   EntityDatabase  database;
        public  readonly    TypeStore       typeStore = new TypeStore();
        public readonly     JsonMapper      jsonMapper;
        
        public EntityStore(EntityDatabase database) {
            this.database = database;
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            jsonMapper = new JsonMapper(typeStore) {
                EntityStore = this
            };
        }
        
        public void Dispose() {
            jsonMapper.Dispose();
            typeStore.Dispose();
        }
        
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly Dictionary<Type, EntitySet> containers = new Dictionary<Type, EntitySet>();

        public async Task Sync() {
            foreach (var container in containers.Values) {
                await container.SyncContainer(database);
            }
        }

        public EntitySet<T> EntitySet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntitySet set))
                return (EntitySet<T>)set;
            
            set = new EntitySet<T>(this);
            return (EntitySet<T>)set;
        }
    }
    

    // ----------------------------------------- CRUD -----------------------------------------
    public class Read<T>
    {
        internal readonly   string id;
        internal            T      result;
        internal            bool   synced;

        internal Read(string id) {
            this.id = id;
        }
            
        public T Result {
            get {
                if (synced)
                    return result;
                throw new InvalidOperationException($"Read.Result requires Sync(). Entity: {typeof(T).Name} id: {id}");
            }
        }
    }
    
    public class Create<T>
    {
        private readonly T             entity;
        private readonly EntityStore   store;

        internal Create(T entity, EntityStore entityStore) {
            this.entity = entity;
            this.store = entityStore;
        }

        public Create<T> Dependencies() {
            var tracer = new Tracer(store.jsonMapper.writer.TypeCache, store);
            tracer.Trace(entity);
            return this;
        }
            
        // public T Result  => entity;
    }

    // ------------------------------------- PeerEntity<> -------------------------------------
    internal class PeerEntity<T>
    {
        internal PeerEntity(T entity) {
            this.entity = entity;
        }
        internal readonly   T      entity;
        internal            bool   assigned;
    }

    public class PeerNotAssignedException : Exception
    {
        public readonly Entity entity;
        
        public PeerNotAssignedException(Entity entity) : base ($"Entity: {entity.GetType().Name} id: {entity.id}") {
            this.entity = entity;
        }
    }

    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        protected internal abstract Task SyncContainer   (EntityDatabase database);
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
            store.containers[typeof(T)] = this;
            jsonMapper = store.jsonMapper;
            typeMapper = (TypeMapper<T>)store.typeStore.GetTypeMapper(typeof(T));
            type = typeof(T);
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

        protected internal override async Task SyncContainer(EntityDatabase database) {
            // creates
            if (creates.Count > 0) {
                List<KeyValue> entries = new List<KeyValue>();
                foreach (var entity in creates) {
                    var entry = new KeyValue {
                        key = entity.id,
                        value = jsonMapper.Write(entity)
                    };
                    entries.Add(entry);
                }
                await container.CreateEntities(entries);
                creates.Clear();
            }
            
            // reads
            if (reads.Count > 0) {
                List<string> ids = new List<string>();
                reads.ForEach(read => ids.Add(read.id));
                var entries = await container.ReadEntities(ids);
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
        
    }
}