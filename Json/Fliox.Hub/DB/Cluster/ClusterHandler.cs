// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    internal sealed class ClusterHandler : TaskHandler
    {
        public override async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, ExecuteContext executeContext)
        {
            // tasks execution for cluster database bypass authorization - access is always allowed by intention.
            // Returned task results are filtered by AuthorizeDatabase instances assigned to the authorizers. 
            // if (!AuthorizeTask(task, executeContext, out var error)) { return error; }
            var clusterDB = (ClusterDB)database;
            switch (task.TaskType) {
                case TaskType.command:
                    return await task.Execute(database, response, executeContext).ConfigureAwait(false);
                case TaskType.read:
                    var read        = (ReadEntities)task;
                    var denied      = ApplyAuthorizedDatabaseFilter(read, executeContext);
                    var readResult  = (ReadEntitiesResult)await task.Execute(clusterDB.stateDB, response, executeContext).ConfigureAwait(false);
                    var entityMap   = response.GetContainerResult(read.container).entityMap;
                    foreach (var id in denied) {
                        entityMap[id] = new EntityValue(new EntityError(EntityErrorType.ReadError, read.container, id, "Permission denied"));
                    }
                    return readResult;
                    // return await task.Execute(clusterDB.stateDB, response, executeContext).ConfigureAwait(false);
                case TaskType.query:
                    ApplyAuthorizedDatabaseFilter((QueryEntities)task, executeContext);
                    return await task.Execute(clusterDB.stateDB, response, executeContext).ConfigureAwait(false);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"ClusterDB does not support task: '{task.TaskType}'");
                    return result;
            }
        }
        
        private static HashSet<JsonKey> ApplyAuthorizedDatabaseFilter(ReadEntities read, ExecuteContext executeContext)
        {
            var deniedIds           = new HashSet<JsonKey>(JsonKey.Equality);
            var authorizedDatabases = Helper.CreateHashSet(4, AuthorizeDatabaseComparer.Instance);
            executeContext.authState.authorizer.AddAuthorizedDatabases(authorizedDatabases);
            foreach (var set in read.sets) {
                var ids = Helper.CreateHashSet(set.ids.Count, JsonKey.Equality);
                foreach (var id in set.ids) {
                    var database    = id.AsString();
                    if (AuthorizeDatabase.IsAuthorizedDatabase(authorizedDatabases, database)) {
                        ids.Add(id);
                    } else {
                        deniedIds.Add(id);
                    }
                }
                set.ids = ids;
            }
            return deniedIds;
        }
        
        private static void ApplyAuthorizedDatabaseFilter(QueryEntities query, ExecuteContext executeContext)
        {
            var authorizedDatabases = Helper.CreateHashSet(4, AuthorizeDatabaseComparer.Instance);
            executeContext.authState.authorizer.AddAuthorizedDatabases(authorizedDatabases);
            var sb = new StringBuilder();
            foreach (var authorizedDatabase in authorizedDatabases) {
                if (sb.Length != 0)
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