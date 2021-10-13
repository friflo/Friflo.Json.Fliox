// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;


namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    public class MonitorHandler : TaskHandler
    {
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (messageContext.customData != this) {
                return base.ExecuteTask(task, database, response, messageContext);
            }
            switch (task.TaskType) {
                case TaskType.read:
                case TaskType.query:
                    return base.ExecuteTask(task, database, response, messageContext);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"MonitorDatabase does not support task: '{task.TaskType}'");
                    return Task.FromResult(result);
            }
        }
    }
}