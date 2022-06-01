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

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    internal sealed class ClusterHandler : TaskHandler
    {
        public override async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext)
        {
            // Note: Keep deprecated comment - may change behavior in future
            //   tasks execution for cluster database bypass authorization - access is always allowed by intention.
            //   Returned task results are filtered by AuthorizeDatabase instances assigned to the authorizers. 
            if (!AuthorizeTask(task, syncContext, out var error)) {
                return error;
            }
            var clusterDB = (ClusterDB)database;
            switch (task.TaskType) {
                case TaskType.command:
                    return await task.Execute(database, response, syncContext).ConfigureAwait(false);
                case TaskType.read:
                    var read        = (ReadEntities)task;
                    var denied      = ApplyAuthorizedDatabaseFilter(read, syncContext);
                    var readResult  = (ReadEntitiesResult)await task.Execute(clusterDB.stateDB, response, syncContext).ConfigureAwait(false);
                    var container   = response.GetContainerResult(read.container);
                    var entityMap   = container.entityMap;
                    foreach (var id in denied) {
                        entityMap.Add(id, new EntityValue());
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
                    return await task.Execute(clusterDB.stateDB, response, syncContext).ConfigureAwait(false);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"ClusterDB does not support task: '{task.TaskType}'");
                    return result;
            }
        }
        
        private static HashSet<JsonKey> ApplyAuthorizedDatabaseFilter(ReadEntities read, SyncContext syncContext)
        {
            var deniedIds           = new HashSet<JsonKey>(JsonKey.Equality);
            var authorizedDatabases = Helper.CreateHashSet(4, AuthorizeDatabaseComparer.Instance);
            syncContext.authState.authorizer.AddAuthorizedDatabases(authorizedDatabases);
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
        
        private static void ApplyAuthorizedDatabaseFilter(QueryEntities query, SyncContext syncContext)
        {
            var authorizedDatabases = Helper.CreateHashSet(4, AuthorizeDatabaseComparer.Instance);
            syncContext.authState.authorizer.AddAuthorizedDatabases(authorizedDatabases);
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