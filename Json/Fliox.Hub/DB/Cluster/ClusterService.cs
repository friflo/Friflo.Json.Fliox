// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    internal sealed class ClusterService : DatabaseService
    {
        internal ClusterDB clusterDB;

        protected internal override void PreExecuteTasks(SyncRequest syncRequest, SyncContext syncContext) {
            var pool = syncContext.pool;
            using (var pooled  = pool.Type(() => new ClusterStore(clusterDB.clusterHub)).Get()) {
                var cluster = pooled.instance;
                var tasks = syncRequest.tasks;
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
                    var read        = (ReadEntities)task;
                    var denied      = ApplyAuthorizedDatabaseFilter(read, syncContext);
                    var readResult  = (ReadEntitiesResult)await task.ExecuteAsync(clusterDB.stateDB, response, syncContext).ConfigureAwait(false);
                    var container   = response.GetContainerResult(read.container);
                    var entityMap   = container.entityMap;
                    foreach (var id in denied) {
                        entityMap.Add(id, new EntityValue(id));
                    }
                    /* var notFound    = container.notFound;
                    if (notFound == null) {
                        container.notFound = notFound = new List<JsonKey>();
                    }
                    notFound.AddRange(denied); */
                    return readResult;
                    // return await task.Execute(clusterDB.stateDB, response, syncContext).ConfigureAwait(false);
                case TaskType.query:
                    ApplyAuthorizedDatabaseFilter((QueryEntities)task, syncContext);
                    return await task.ExecuteAsync(clusterDB.stateDB, response, syncContext).ConfigureAwait(false);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"ClusterDB does not support task: '{task.TaskType}'");
                    return result;
            }
        }
        
        private static HashSet<JsonKey> ApplyAuthorizedDatabaseFilter(ReadEntities read, SyncContext syncContext)
        {
            var deniedIds       = new HashSet<JsonKey>(JsonKey.Equality);
            var databaseFilters = Helper.CreateHashSet(4, DatabaseFilterComparer.Instance);
            syncContext.authState.taskAuthorizer.AddAuthorizedDatabases(databaseFilters);

            var ids = new List<JsonKey>(read.ids.Count);
            foreach (var id in read.ids) {
                var database    = new SmallString(id.AsString());
                if (DatabaseFilter.IsAuthorizedDatabase(databaseFilters, database)) {
                    ids.Add(id);
                } else {
                    deniedIds.Add(id);
                }
            }
            read.ids = ids;
            return deniedIds;
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
                    sb.Append($"(o.id.StartsWith('{authorizedDatabase.database}'))");
                else 
                    sb.Append($"(o.id == '{authorizedDatabase.database}')");
            }
            query.filter        = sb.ToString();
            query.filterTree    = default;
            // Console.WriteLine(query.filter);
        }
    }
}