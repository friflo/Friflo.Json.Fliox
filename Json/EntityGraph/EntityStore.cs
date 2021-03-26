// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        internal readonly   EntityDatabase      database;
        public   readonly   TypeStore           typeStore = new TypeStore();
        public   readonly   JsonMapper          jsonMapper;

        
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
            SyncRequest syncRequest = CreateSyncRequest();
            SyncResponse response = await Task.Run(() => database.Execute(syncRequest)); // <--- asynchronous Sync Point
#if DEBUG
            var jsonSync = jsonMapper.Write(syncRequest); // todo remove - log StoreSyncRequest as JSON
#endif
            HandleSyncRequest(syncRequest, response);
        }
        
        public void SyncWait() {
            SyncRequest syncRequest = CreateSyncRequest();
            SyncResponse response = database.Execute(syncRequest); // <--- synchronous Sync Point
            HandleSyncRequest(syncRequest, response);
        }

        private SyncRequest CreateSyncRequest() {
            var syncRequest = new SyncRequest { commands = new List<DatabaseCommand>() };
            foreach (var setPair in setByType) {
                EntitySet set = setPair.Value;
                set.AddCommands(syncRequest.commands);
            }
            return syncRequest;
        }

        private void HandleSyncRequest(SyncRequest syncRequest, SyncResponse response) {
            var commands = syncRequest.commands;
            var results = response.results;
            for (int n = 0; n < commands.Count; n++) {
                var command = commands[n];
                var result = results[n];
                CommandType commandType = command.CommandType;
                switch (commandType) {
                    case CommandType.Create:
                        var create = (CreateEntities) command;
                        EntitySet set = setByName[create.containerName];
                        set.CreateEntitiesResult(create, (CreateEntitiesResult)result);
                        break;
                    case CommandType.Read:
                        var read = (ReadEntities) command;
                        set = setByName[read.containerName];
                        set.ReadEntitiesResult(read, (ReadEntitiesResult)result);
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
        private  readonly   string id;
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
}