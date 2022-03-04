// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;


namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    internal sealed class MonitorHandler : TaskHandler
    {
        private readonly   FlioxHub            hub;
        
        internal MonitorHandler (FlioxHub hub) {
            this.hub = hub;
            AddCommand<ClearStats, ClearStatsResult> (nameof(ClearStats), ClearStats);
        }
        
        internal ClearStatsResult ClearStats(Param<ClearStats> param, Command command) {
            // clear request counts of the hub. Extension databases share the same hub.
            hub.Authenticator.ClearUserStats();
            hub.ClientController.ClearClientStats();
            hub.hostStats.ClearHostStats();
            return new ClearStatsResult();
        }
        
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (!AuthorizeTask(task, messageContext, out var error)) {
                return Task.FromResult(error);
            }
            var monitorDB = (MonitorDB)database;
            switch (task.TaskType) {
                case TaskType.command:
                    return base.ExecuteTask(task, database, response, messageContext);
                case TaskType.read:
                case TaskType.query:
                    return base.ExecuteTask(task, monitorDB.stateDB, response, messageContext);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"MonitorDB does not support task: '{task.TaskType}'");
                    return Task.FromResult(result);
            }
        }
    }
}