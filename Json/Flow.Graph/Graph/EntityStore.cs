// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Graph.Internal.Map;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Graph
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
        // private  readonly   JsonReadError                errorHandler;

        internal readonly   ObjectPatcher                   objectPatcher;
        
        internal readonly   EntityDatabase                  database;
        internal readonly   Dictionary<Type,   EntitySet>   setByType;
        internal readonly   Dictionary<string, EntitySet>   setByName;
        
        internal StoreIntern(TypeStore typeStore, EntityDatabase database, ObjectMapper jsonMapper) {
            this.typeStore      = typeStore;
            this.database       = database;
            this.jsonMapper     = jsonMapper;
            this.typeCache      = jsonMapper.writer.TypeCache;
            setByType           = new Dictionary<Type, EntitySet>();
            setByName           = new Dictionary<string, EntitySet>();
            objectPatcher       = new ObjectPatcher(jsonMapper);
        }
    }

    internal class JsonReadError : IErrorHandler
    {
        /// throw no exceptions on errors. Errors are handled by checking <see cref="ObjectReader.Success"/> 
        public void HandleError(int pos, ref Bytes message) {
        }
    }
    
    // --------------------------------------- EntityStore ---------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class EntityStore : ITracerContext, IDisposable
    {
        // Keep all EntityStore fields in StoreIntern to enhance debugging overview.
        // Reason: EntityStore is extended by application and add multiple EntitySet fields.
        //         So internal fields are encapsulated in field intern.
        // ReSharper disable once InconsistentNaming
        internal readonly   StoreIntern     _intern;
        public              TypeStore       TypeStore => _intern.typeStore;
        internal            SyncStore       sync;
        internal            LogTask         logTask;

        public              StoreInfo       StoreInfo  => new StoreInfo(_intern.setByType); 
        public   override   string          ToString() => StoreInfo.ToString();


        protected EntityStore(EntityDatabase database) {
            var typeStore = new TypeStore();
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            var errorHandler = new JsonReadError();
            var jsonMapper = new ObjectMapper(typeStore, errorHandler) {
                TracerContext = this
            };
            sync = new SyncStore();
            _intern = new StoreIntern(typeStore, database, jsonMapper);
        }
        
        public void Dispose() {
            _intern.objectPatcher.Dispose();
            _intern.jsonMapper.Dispose();
            _intern.typeStore.Dispose();
        }

        public async Task Sync() {
            SyncRequest syncRequest = CreateSyncRequest();

            SyncResponse response = await _intern.database.Execute(syncRequest); // <--- asynchronous Sync point
            HandleSyncResponse(syncRequest, response);
        }
        
        public void SyncWait() {
            SyncRequest syncRequest = CreateSyncRequest();
            var responseTask = _intern.database.Execute(syncRequest);
            // responseTask.Wait();  
            SyncResponse response = responseTask.Result;  // <--- synchronous Sync point
            HandleSyncResponse(syncRequest, response);
        }

        public LogTask LogChanges() {
            var logTask = sync.CreateLog();
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.LogSetChangesInternal(logTask);
            }
            return logTask;
        }
        
        internal EntitySet<T> EntitySet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (_intern.setByType.TryGetValue(entityType, out EntitySet set))
                return (EntitySet<T>)set;
            
            set = new EntitySet<T>(this);
            return (EntitySet<T>)set;
        }

        private SyncRequest CreateSyncRequest() {
            var syncRequest = new SyncRequest { tasks = new List<DatabaseTask>() };
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                var setInfo = set.SetInfo;
                var curTaskCount = syncRequest.tasks.Count;
                set.Sync.AddTasks(syncRequest.tasks);
                AssertTaskCount(setInfo, syncRequest.tasks.Count - curTaskCount);
            }
            return syncRequest;
        }

        [Conditional("DEBUG")]
        private static void AssertTaskCount(SetInfo setInfo, int taskCount) {
            int expect  = setInfo.tasks; 
            if (expect != taskCount)
                throw new InvalidOperationException($"Unexpected task.Count. expect: {expect}, got: {taskCount}");
        }

        private void HandleSyncResponse(SyncRequest syncRequest, SyncResponse response) {
            response.AssertResponse(syncRequest);
            try {
                var containerResults = response.results;
                foreach (var containerResult in containerResults) {
                    var set = _intern.setByName[containerResult.Key];
                    set.SyncContainerEntities(containerResult.Value);
                }
                var createErrors = response.createErrors;
                foreach (var createError in createErrors) {
                    var set = _intern.setByName[createError.Key];
                    set.SyncCreateErrors(createError.Value);
                }
                var patchErrors = response.patchErrors;
                foreach (var patchError in patchErrors) {
                    var set = _intern.setByName[patchError.Key];
                    set.SyncPatchErrors(patchError.Value);
                }

                var tasks   = syncRequest.tasks;
                var results = response.tasks;
                for (int n = 0; n < tasks.Count; n++) {
                    var task = tasks[n];
                    var result = results[n];
                    TaskType taskType = task.TaskType;
                    var actual = result.TaskType;
                    if (actual != TaskType.Error) {
                        if (taskType != actual) {
                            var msg = $"Expect task type of response matches request. index:{n} expect: {taskType} actual: {actual}";
                            throw new InvalidOperationException(msg);
                        }
                    }

                    switch (taskType) {
                        case TaskType.Create:
                            var create = (CreateEntities) task;
                            EntitySet set = _intern.setByName[create.container];
                            set.Sync.CreateEntitiesResult(create, result);
                            break;
                        case TaskType.Update:
                            var update = (UpdateEntities) task;
                            set = _intern.setByName[update.container];
                            set.Sync.UpdateEntitiesResult(update, result);
                            break;
                        case TaskType.Read:
                            var readList = (ReadEntitiesList) task;
                            set = _intern.setByName[readList.container];
                            containerResults.TryGetValue(readList.container, out ContainerEntities entities);
                            set.Sync.ReadEntitiesListResult(readList, result, entities);
                            break;
                        case TaskType.Query:
                            var query = (QueryEntities) task;
                            set = _intern.setByName[query.container];
                            containerResults.TryGetValue(query.container, out ContainerEntities queryEntities);
                            set.Sync.QueryEntitiesResult(query, result, queryEntities);
                            break;
                        case TaskType.Patch:
                            var patch = (PatchEntities) task;
                            set = _intern.setByName[patch.container];
                            set.Sync.PatchEntitiesResult(patch, result);
                            break;
                        case TaskType.Delete:
                            var delete = (DeleteEntities) task;
                            set = _intern.setByName[delete.container];
                            set.Sync.DeleteEntitiesResult(delete, result);
                            break;
                    }
                }
                sync.LogResults();
            }
            finally
            {
                // new EntitySet task are collected (scheduled) in a new EntitySetSync instance and requested via next Sync() 
                foreach (var setPair in _intern.setByType) {
                    EntitySet set = setPair.Value;
                    set.ResetSync();
                }
                sync = new SyncStore();
            }
        }
    }
}
