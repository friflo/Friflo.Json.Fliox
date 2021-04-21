// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.EntityGraph.Internal.Map;
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
            SyncResponse response = await intern.database.Execute(syncRequest); // <--- asynchronous Sync point
            HandleSyncRequest(syncRequest, response);
        }
        
        public void SyncWait() {
            SyncRequest syncRequest = CreateSyncRequest();
            var responseTask = intern.database.Execute(syncRequest);
            // responseTask.Wait();  
            SyncResponse response = responseTask.Result;  // <--- synchronous Sync point
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
            var syncRequest = new SyncRequest { tasks = new List<DatabaseTask>() };
            foreach (var setPair in intern.setByType) {
                EntitySet set = setPair.Value;
                set.Sync.AddTasks(syncRequest.tasks);
            }
            return syncRequest;
        }

        private void HandleSyncRequest(SyncRequest syncRequest, SyncResponse response) {
            try {
                foreach (var containerResults in response.containerResults) {
                    var set = intern.setByName[containerResults.Key];
                    set.SyncEntities(containerResults.Value);
                }

                var tasks = syncRequest.tasks;
                var results = response.results;
                for (int n = 0; n < tasks.Count; n++) {
                    var task = tasks[n];
                    var result = results[n];
                    TaskType taskType = task.TaskType;
                    if (taskType != result.TaskType)
                        throw new InvalidOperationException($"Expect CommandType of response matches request. index:{n} expect: {taskType} got: {result.TaskType}");
                    switch (taskType) {
                        case TaskType.Create:
                            var create = (CreateEntities) task;
                            EntitySet set = intern.setByName[create.container];
                            set.Sync.CreateEntitiesResult(create, (CreateEntitiesResult) result);
                            break;
                        case TaskType.Read:
                            var read = (ReadEntities) task;
                            set = intern.setByName[read.container];
                            set.Sync.ReadEntitiesResult(read, (ReadEntitiesResult) result);
                            break;
                        case TaskType.Query:
                            var query = (QueryEntities) task;
                            set = intern.setByName[query.container];
                            set.Sync.QueryEntitiesResult(query, (QueryEntitiesResult) result);
                            break;
                        case TaskType.Patch:
                            var patch = (PatchEntities) task;
                            set = intern.setByName[patch.container];
                            set.Sync.PatchEntitiesResult(patch, (PatchEntitiesResult) result);
                            break;
                        case TaskType.Delete:
                            var delete = (DeleteEntities) task;
                            set = intern.setByName[delete.container];
                            set.Sync.DeleteEntitiesResult(delete, (DeleteEntitiesResult) result);
                            break;
                    }
                }
            }
            finally
            {
                // new EntitySet task are collected (scheduled) in a new EntitySetSync instance and requested via next Sync() 
                foreach (var setPair in intern.setByType) {
                    EntitySet set = setPair.Value;
                    set.ResetSync();
                }
            }
        }
    }
}
