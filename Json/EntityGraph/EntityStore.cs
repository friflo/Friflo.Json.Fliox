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
    public interface ISyncHandler {
        Task ExecuteSync(EntityDatabase database, StoreSyncRequest request);
    }

    public class SyncHandler : ISyncHandler
    {
#pragma warning disable 1998  // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await TaskEx.Run(...)' to do CPU-bound work on a background thread
        public async Task ExecuteSync(EntityDatabase database, StoreSyncRequest syncRequest) { 
            syncRequest.Execute(database);
        }
    }
#pragma warning restore 1998
    
    public class BackgroundSyncHandler : ISyncHandler
    {
        public async Task ExecuteSync(EntityDatabase database, StoreSyncRequest syncRequest) {
            await Task.Run(() => {
                syncRequest.Execute(database);
            });
        }
    }

    public static class StoreExtension
    {
        public static EntityStore Store(this ITracerContext store) {
            return (EntityStore)store;
        }
    }
    
    // --------------------------------------- EntityStore ---------------------------------------
    public class EntityStore : ITracerContext, IDisposable
    {
        internal readonly   EntityDatabase      database;
        public   readonly   TypeStore           typeStore = new TypeStore();
        public   readonly   JsonMapper          jsonMapper;
        private  readonly   ISyncHandler        syncHandler = new BackgroundSyncHandler();

        
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
            var storeRequest = new StoreSyncRequest { requests = new List<StoreRequest>() };
            foreach (var setPair in setByType) {
                EntitySet set = setPair.Value;
                set.AddSetRequests(storeRequest);
            }

            await syncHandler.ExecuteSync(database, storeRequest);
            
            // ---> async Sync Point!
#if DEBUG
            var jsonSync = jsonMapper.Write(storeRequest); // todo remove - log StoreSyncRequest as JSON
#endif
            HandleSetResponse(storeRequest);
        }
        
        private void HandleSetResponse(StoreSyncRequest syncRequest) {
            var requests = syncRequest.requests;
            foreach (var request in requests) {
                RequestType requestType = request.RequestType;
                switch (requestType) {
                    case RequestType.Create:
                        var create = (CreateEntitiesRequest) request;
                        EntitySet set = setByName[create.containerName];
                        set.CreateEntitiesResponse(create);
                        break;
                    case RequestType.Read:
                        var read = (ReadEntitiesRequest) request;
                        set = setByName[read.containerName];
                        set.ReadEntitiesResponse(read);
                        break;
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
                throw new InvalidOperationException($"Read().Result requires Sync(). Entity: {typeof(T).Name} id: {id}");
            }
        }
    }
    
    public class Create<T>
    {
        private readonly    T           entity;
        private readonly    EntityStore store;

        internal            T           Entity => entity;
        
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
    internal class PeerEntity<T>  where T : Entity
    {
        internal readonly   T           entity;
        internal            bool        assigned;
        internal            Read<T>     read;
        internal            Create<T>   create;

        internal PeerEntity(T entity) {
            this.entity = entity;
        }
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

        public void Execute(EntityDatabase database) {
            foreach (var request in requests) {
                request.Execute(database);
            }
        }
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