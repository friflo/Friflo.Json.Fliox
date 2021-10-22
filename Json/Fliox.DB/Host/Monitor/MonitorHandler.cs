// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;


namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    internal sealed class MonitorHandler : TaskHandler
    {
        internal readonly MonitorDatabase monitorDB;
        
        internal MonitorHandler(MonitorDatabase monitorDB) {
            this.monitorDB = monitorDB;
            AddCommandHandler<ClearStats, ClearStatsResult>(ClearStats);
        }
        
        private ClearStatsResult ClearStats(Command<ClearStats> command) {
            // clear request counts of default database. They contain also request counts of all extension databases.
            var baseDB = monitorDB.hub;
            baseDB.Authenticator.ClearUserStats();
            baseDB.ClientController.ClearClientStats();
            baseDB.hostStats.ClearHostStats();
            return new ClearStatsResult();
        }
        
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            switch (task.TaskType) {
                case TaskType.message:
                case TaskType.read:
                case TaskType.query:
                    return base.ExecuteTask(task, monitorDB.stateDB, response, messageContext);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"MonitorDatabase does not support task: '{task.TaskType}'");
                    return Task.FromResult(result);
            }
        }
    }
}