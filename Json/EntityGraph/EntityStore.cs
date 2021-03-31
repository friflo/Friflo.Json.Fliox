// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.EntityGraph.Map;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Diff;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.EntityGraph
{
    public static class StoreExtension
    {
        public static EntityStore Store(this ITracerContext store) {
            return (EntityStore)store;
        }
    }

    public readonly struct StoreIntern
    {
        public   readonly   TypeStore                       typeStore;
        public   readonly   TypeCache                       typeCache;
        public   readonly   JsonMapper                      jsonMapper;

        internal readonly   ObjectPatcher                   objectPatcher;
        
        internal readonly   EntityDatabase                  database;
        internal readonly   Dictionary<Type,   EntitySet>   setByType;
        internal readonly   Dictionary<string, EntitySet>   setByName;

        internal StoreIntern(TypeStore typeStore, EntityDatabase database, JsonMapper jsonMapper) {
            this.typeStore  = typeStore;
            this.database   = database;
            this.jsonMapper = jsonMapper;
            this.typeCache  = jsonMapper.writer.TypeCache;
            setByType = new Dictionary<Type, EntitySet>();
            setByName = new Dictionary<string, EntitySet>();
            objectPatcher = new ObjectPatcher(jsonMapper);
        } 
    }
    
    // --------------------------------------- EntityStore ---------------------------------------
    public class EntityStore : ITracerContext, IDisposable
    {
        // Keep all EntityStore fields in StoreIntern to enhance debugging overview.
        // Reason: EntityStore is extended by application and add multiple EntitySet fields.
        //         So internal fields are encapsulated in field intern.
        public readonly StoreIntern   intern;
        
        public EntityStore(EntityDatabase database) {
            var typeStore = new TypeStore();
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            var jsonMapper = new JsonMapper(typeStore) {
                TracerContext = this
            };
            intern = new StoreIntern(typeStore, database, jsonMapper);
        }
        
        public void Dispose() {
            intern.objectPatcher.Dispose();
            intern.jsonMapper.Dispose();
            intern.typeStore.Dispose();
        }

        public async Task Sync() {
            SyncRequest syncRequest = CreateSyncRequest();
            SyncResponse response = await Task.Run(() => intern.database.Execute(syncRequest)); // <--- asynchronous Sync point
            HandleSyncRequest(syncRequest, response);
        }
        
        public void SyncWait() {
            SyncRequest syncRequest = CreateSyncRequest();
            SyncResponse response = intern.database.Execute(syncRequest); // <--- synchronous Sync point
            HandleSyncRequest(syncRequest, response);
        }

        private SyncRequest CreateSyncRequest() {
            var syncRequest = new SyncRequest { commands = new List<DatabaseCommand>() };
            foreach (var setPair in intern.setByType) {
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
                if (commandType != result.CommandType)
                    throw new InvalidOperationException($"Expect CommandType of response matches request. index:{n} expect: {commandType} got: {result.CommandType}");
                switch (commandType) {
                    case CommandType.Create:
                        var create = (CreateEntities) command;
                        EntitySet set = intern.setByName[create.container];
                        set.CreateEntitiesResult(create, (CreateEntitiesResult)result);
                        break;
                    case CommandType.Read:
                        var read = (ReadEntities) command;
                        set = intern.setByName[read.container];
                        set.ReadEntitiesResult(read, (ReadEntitiesResult)result);
                        break;
                    case CommandType.Patch:
                        var patch = (PatchEntities) command;
                        set = intern.setByName[patch.container];
                        set.PatchEntitiesResult(patch, (PatchEntitiesResult)result);
                        break;
                }
            }
        }

        public EntitySet<T> EntitySet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (intern.setByType.TryGetValue(entityType, out EntitySet set))
                return (EntitySet<T>)set;
            
            set = new EntitySet<T>(this);
            return (EntitySet<T>)set;
        }
        
        public int SaveChanges() {
            int count = 0;
            foreach (var setPair in intern.setByType) {
                EntitySet set = setPair.Value;
                count += set.SaveSetChanges();
            }
            return count;
        }
    }
}
