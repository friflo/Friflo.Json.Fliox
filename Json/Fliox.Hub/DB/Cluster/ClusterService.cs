// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Language;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    internal sealed class ClusterService : DatabaseService
    {
        internal            ClusterDB                               clusterDB;
        private   readonly  Dictionary<string,List<SchemaModel>>    schemaModelsMap;
        
        internal ClusterService() {
            AddCommandHandler<ModelFilesQuery, List<ModelFiles>> (nameof(ModelFiles), ModelFiles);
            schemaModelsMap = new Dictionary<string, List<SchemaModel>>();
        }

        protected internal override void PreExecuteTasks(SyncContext syncContext) {
            var pool = syncContext.pool;
            using (var pooled  = pool.Type(() => new ClusterStore(clusterDB.clusterHub)).Get()) {
                var cluster = pooled.instance;
                var tasks = syncContext.request.tasks;
                cluster.UpdateClusterDB  (clusterDB.hub, tasks);
                
                cluster.SyncTasksSynchronous();
            }
        }
        
        public override async Task<SyncTaskResult> ExecuteTaskAsync (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext)
        {
            // Note: Keep deprecated comment - may change behavior in future
            //   tasks execution for cluster database bypass authorization - access is always allowed by intention.
            //   Returned task results are filtered by AuthorizeDatabase instances assigned to the authorizers. 
            if (!AuthorizeTask(task, syncContext, out var error)) {
                return error;
            }
            switch (task.TaskType) {
                case TaskType.command:
                    return await task.ExecuteAsync(database, response, syncContext).ConfigureAwait(false);
                case TaskType.read:
                    var read = (ReadEntities)task;
                    ApplyAuthorizedDatabaseFilter(read, syncContext);
                    return (ReadEntitiesResult)await task.ExecuteAsync(clusterDB.stateDB, response, syncContext).ConfigureAwait(false);
                case TaskType.query:
                    ApplyAuthorizedDatabaseFilter((QueryEntities)task, syncContext);
                    return await task.ExecuteAsync(clusterDB.stateDB, response, syncContext).ConfigureAwait(false);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"ClusterDB does not support task: '{task.TaskType}'");
                    return result;
            }
        }
        /// <summary>
        /// Remove all ids from given <paramref name="read"/> task the user ist not authorized to access,  
        /// </summary>
        private static void ApplyAuthorizedDatabaseFilter(ReadEntities read, SyncContext syncContext)
        {
            var databaseFilters = Helper.CreateHashSet(4, DatabaseFilterComparer.Instance);
            syncContext.authState.taskAuthorizer.AddAuthorizedDatabases(databaseFilters);

            var ids = new ListOne<JsonKey>(read.ids.Count);
            foreach (var id in read.ids.GetReadOnlySpan()) {
                var idShort = new ShortString(id);
                if (DatabaseFilter.IsAuthorizedDatabase(databaseFilters, idShort)) {
                    ids.Add(id);
                }
            }
            read.ids = ids;
        }
        
        private static void ApplyAuthorizedDatabaseFilter(QueryEntities query, SyncContext syncContext)
        {
            var authorizedDatabases = Helper.CreateHashSet(4, DatabaseFilterComparer.Instance);
            syncContext.authState.taskAuthorizer.AddAuthorizedDatabases(authorizedDatabases);
            var sb = new StringBuilder();
            foreach (var authorizedDatabase in authorizedDatabases) {
                if (sb.Length == 0)
                    sb.Append("o => ");
                else
                    sb.Append(" || ");
                if (authorizedDatabase.isPrefix)
                    sb.Append($"(o.id.StartsWith('{authorizedDatabase.database.AsString()}'))");
                else 
                    sb.Append($"(o.id == '{authorizedDatabase.database.AsString()}')");
            }
            query.filter        = sb.ToString();
            query.filterTree    = default;
            // Console.WriteLine(query.filter);
        }
        
        internal Result<List<ModelFiles>> ModelFiles (Param<ModelFilesQuery> param, MessageContext context) {
            if (!param.GetValidate(out var query, out string error)) {
                return Result.Error(error);
            }
            var allDatabases = context.Hub.GetDatabases();
            EntityDatabase[] databases;
            if (query?.db != null) {
                if (!allDatabases.TryGetValue(query.db, out var database)) {
                    return Result.Error($"database not found: {query.db}");
                }
                databases = new [] { database };
            } else {
                databases = allDatabases.Values.ToArray();
            }
            var result        = new List<ModelFiles>();
            foreach (var database in databases) {
                var dbName = database.name;
                if (!schemaModelsMap.TryGetValue(dbName, out var schemaModels)) {
                    var schema          = database.Schema;
                    var entityTypeMap   = schema.typeSchema.GetEntityTypes();
                    var entityTypes     = entityTypeMap.Values;
                    schemaModels        = SchemaModel.GenerateSchemaModels (schema.typeSchema, entityTypes);
                    schemaModelsMap.Add(dbName, schemaModels);
                }
                foreach (var model in schemaModels) {
                    if (query?.type != null && query?.type != model.type) {
                        continue;
                    }
                    var modelFiles = new ModelFiles {
                        db          = dbName,
                        type        = model.type,
                        label       = model.label,
                        files       = new List<ModelFile>()
                    };
                    foreach (var file in model.files) {
                        var typeModel = new ModelFile { path = file.Key, content = file.Value };
                        modelFiles.files.Add(typeModel);
                    }
                    result.Add(modelFiles);
                }
            }
            return result;
        }
    }
}