// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.EntityGraph.Map;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;

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
        internal readonly Dictionary<Type,   EntitySet> setByType = new Dictionary<Type, EntitySet>();
        internal readonly Dictionary<string, EntitySet> setByName = new Dictionary<string, EntitySet>();

        public async Task Sync() {
            var storeRequest = new StoreSyncRequest();
            foreach (var container in setByType.Values) {
                container.CreateStoreRequest(storeRequest);
                // async Sync Point!
                foreach (var request in storeRequest.requests) {
                    request.Execute(database);
                }
                foreach (var request in storeRequest.requests) {
                    RequestType type = request.RequestType;
                    switch (type) {
                        case RequestType.Create:
                            var create = (CreateEntitiesRequest) request;
                            EntitySet set = setByName[create.containerName];
                            set.CreateEntities(create);
                            break;
                        case RequestType.Read:
                            var read = (ReadEntitiesRequest) request;
                            set = setByName[read.containerName];
                            set.ReadEntities(read);
                            break;
                        
                    }
                }
            }
        }

        public EntitySet<T> EntitySet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (setByType.TryGetValue(entityType, out EntitySet set))
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


    // ----------------------------------- StoreSyncRequest -----------------------------------
    public class StoreSyncRequest
    {
        public List<StoreRequest> requests = new List<StoreRequest>();
    }

    [Fri.Discriminator("request")]
    [Fri.Polymorph(typeof(CreateEntitiesRequest),  Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntitiesRequest),    Discriminant = "read")]
    public abstract class StoreRequest
    {
        public abstract void        Execute(EntityDatabase database);
        public abstract RequestType RequestType { get; }
    }
    
    public class CreateEntitiesRequest : StoreRequest
    {
        public  string              containerName;
        public  List<KeyValue>      entities;

        public override RequestType RequestType => RequestType.Create;

        public override void Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
            container.CreateEntities(entities);   
        }
    }
    
    public class ReadEntitiesRequest : StoreRequest
    {
        public  string              containerName;
        public  List<string>        ids;
        [Fri.Ignore]
        public  List<KeyValue>      entitiesResult;
        
        public override RequestType RequestType => RequestType.Read;
        
        public override void Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
            entitiesResult = container.ReadEntities(ids).ToList(); 
        }
    }

    public enum RequestType
    {
        Read,
        Create
    }

}