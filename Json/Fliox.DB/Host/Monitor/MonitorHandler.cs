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
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var monitorDB = (MonitorDatabase)database;
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