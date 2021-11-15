// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;


namespace Friflo.Json.Fliox.Hub.Host.Cluster
{
    internal sealed class ClusterHandler : TaskHandler
    {
        private readonly   FlioxHub            hub;
        
        internal ClusterHandler (FlioxHub hub) {
            this.hub = hub;

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