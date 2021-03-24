// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.EntityGraph.Map;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph
{

    public static class StoreExtension
    {
        public static EntityStore Store(this ITracerContext store) {
            return (EntityStore)store;
        }
    }
    
    // --------------------------------------- EntityStore ---------------------------------------
    public class EntityStore : ITracerContext, IDisposable
    {
        internal readonly   EntityDatabase  database;
        public  readonly    TypeStore       typeStore = new TypeStore();
        public readonly     JsonMapper      jsonMapper;
        
        public EntityStore(EntityDatabase database) {
            this.database = database;
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            jsonMapper = new JsonMapper(typeStore) {
                TracerContext = this
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
                container.SyncContainer();
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


    // ------------------------------------- Store Request -------------------------------------
    public class StoreRequest
    {
        public readonly    List<SetRequest>                sets     = new List<SetRequest>(); 
    }
    
    public class SetRequest
    {
        public readonly    List<string>                        readIds  = new List<string>();
        public readonly    List<CreateEntityRequest>           creates  = new List<CreateEntityRequest>(); 
    }
    
    public class CreateEntityRequest
    {
        public readonly    string                              ids;
        public readonly    JsonValue                           payload; 
    }
}