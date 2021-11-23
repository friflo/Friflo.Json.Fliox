// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    internal sealed class ClusterHandler : TaskHandler
    {
        private   readonly  FlioxHub hub;
        
        public ClusterHandler (FlioxHub hub) {
            this.hub = hub;
            AddCommandHandler<Empty, CatalogList>(StdCommand.CatalogList, CatalogList);
        }
        
        private CatalogList CatalogList (Command<Empty> command) {
            var databases = hub.GetDatabases();
            var catalogs = new List<Catalog>(databases.Count);
            foreach (var pair in databases) {
                var database        = pair.Value;
                var databaseInfo    = database.GetDatabaseInfo();
                var catalog = new Catalog {
                    id              = pair.Key,
                    databaseType    = databaseInfo.databaseType,
                    containers      = databaseInfo.containers
                };
                catalogs.Add(catalog);
            }
            return new CatalogList{ catalogs = catalogs };
        }
        
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (!AuthorizeTask(task, messageContext, out var error)) {
                return Task.FromResult(error);
            }
            var clusterDB = (ClusterDB)database;
            switch (task.TaskType) {
                case TaskType.command:
                    return base.ExecuteTask(task, database, response, messageContext);
                case TaskType.read:
                case TaskType.query:
                    return base.ExecuteTask(task, clusterDB.stateDB, response, messageContext);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"ClusterDB does not support task: '{task.TaskType}'");
                    return Task.FromResult(result);
            }
        }
    }
}