// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.EntityGraph.Map;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.EntityGraph
{
    public static class StoreExtension
    {
        public static EntityStore Store(this ITracerContext store) {
            return (EntityStore)store;
        }
    }

    internal readonly struct StoreIntern
    {
        internal readonly   TypeStore                       typeStore;
        internal readonly   TypeCache                       typeCache;
        internal readonly   ObjectMapper                    jsonMapper;

        internal readonly   ObjectPatcher                   objectPatcher;
        
        internal readonly   EntityDatabase                  database;
        internal readonly   Dictionary<Type,   EntitySet>   setByType;
        internal readonly   Dictionary<string, EntitySet>   setByName;

        internal StoreIntern(TypeStore typeStore, EntityDatabase database, ObjectMapper jsonMapper) {
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
        internal readonly   StoreIntern     intern;
        public              TypeStore       TypeStore => intern.typeStore;
        
        protected EntityStore(EntityDatabase database) {
            var typeStore = new TypeStore();
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            var jsonMapper = new ObjectMapper(typeStore) {
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

        public int LogChanges() {
            int count = 0;
            foreach (var setPair in intern.setByType) {
                EntitySet set = setPair.Value;
                count += set.LogSetChanges();
            }
            return count;
        }
        
        internal EntitySet<T> EntitySet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (intern.setByType.TryGetValue(entityType, out EntitySet set))
                return (EntitySet<T>)set;
            
            set = new EntitySet<T>(this);
            return (EntitySet<T>)set;
        }

        private SyncRequest CreateSyncRequest() {
            var syncRequest = new SyncRequest { commands = new List<DbCommand>() };
            foreach (var setPair in intern.setByType) {
                EntitySet set = setPair.Value;
                set.Sync.AddCommands(syncRequest.commands);
            }
            return syncRequest;
        }

        private void HandleSyncRequest(SyncRequest syncRequest, SyncResponse response) {
            foreach (var containerResults in response.containerResults) {
                var set = intern.setByName[containerResults.Key];
                set.SyncEntities(containerResults.Value);
            }
            
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
                        set.Sync.CreateEntitiesResult(create, (CreateEntitiesResult)result);
                        break;
                    case CommandType.Read:
                        var read = (ReadEntities) command;
                        set = intern.setByName[read.container];
                        set.Sync.ReadEntitiesResult(read, (ReadEntitiesResult)result);
                        break;
                    case CommandType.Query:
                        var query = (QueryEntities) command;
                        set = intern.setByName[query.container];
                        set.Sync.QueryEntitiesResult(query, (QueryEntitiesResult)result);
                        break;
                    case CommandType.Patch:
                        var patch = (PatchEntities) command;
                        set = intern.setByName[patch.container];
                        set.Sync.PatchEntitiesResult(patch, (PatchEntitiesResult)result);
                        break;
                }
            }
            
            // new EntitySet task are collected (scheduled) in a new EntitySetSync instance and requested via next Sync() 
            foreach (var setPair in intern.setByType) {
                EntitySet set = setPair.Value;
                set.ResetSync();
            }
        }
    }
}
