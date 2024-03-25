// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;


namespace Friflo.Json.Fliox.Hub.DB.Monitor
{
    internal sealed class MonitorService : DatabaseService
    {
        internal            MonitorDB   monitorDB;
        private  readonly   FlioxHub    hub;
        
        internal MonitorService (FlioxHub hub) {
            this.hub = hub;
            AddCommandHandler<ClearStats, ClearStatsResult> (nameof(ClearStats), ClearStats);
        }
        
        protected internal override void PreExecuteTasks(SyncContext syncContext) {
            var pool = syncContext.pool;
            using (var pooled  = pool.Type(() => new MonitorStore(monitorDB.monitorHub)).Get()) {
                var monitor = pooled.instance;
                var tasks = syncContext.request.tasks;
                if (MonitorDB.FindTask(nameof(MonitorStore.clients),  tasks)) monitor.UpdateClients  (hub, monitorDB.name);
                if (MonitorDB.FindTask(nameof(MonitorStore.users),    tasks)) monitor.UpdateUsers    (hub.Authenticator, monitorDB.name);
                if (MonitorDB.FindTask(nameof(MonitorStore.histories),tasks)) monitor.UpdateHistories(hub.hostStats.requestHistories);
                if (MonitorDB.FindTask(nameof(MonitorStore.hosts),    tasks)) monitor.UpdateHost     (hub, hub.hostStats);
                
                monitor.TrySyncTasksSynchronous();
            }
        }
        
        internal Result<ClearStatsResult> ClearStats(Param<ClearStats> param, MessageContext context) {
            // clear request counts of the hub. Extension databases share the same hub.
            hub.Authenticator.ClearUserStats();
            hub.ClientController.ClearClientStats();
            hub.hostStats.ClearHostStats();
            return new ClearStatsResult();
        }
        
        public override Task<SyncTaskResult> ExecuteTaskAsync (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (!AuthorizeTask(task, syncContext, out var error)) {
                return Task.FromResult(error);
            }
            switch (task.TaskType) {
                case TaskType.command:
                    return base.ExecuteTaskAsync(task, database, response, syncContext);
                case TaskType.read:
                case TaskType.query:
                    return base.ExecuteTaskAsync(task, monitorDB.stateDB, response, syncContext);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"MonitorDB does not support task: '{task.TaskType}'");
                    return Task.FromResult(result);
            }
        }
    }
}