// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;


namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    public class MonitorHandler : TaskHandler
    {
        readonly MonitorDatabase monitorDb;
        
        internal MonitorHandler(MonitorDatabase monitorDb) {
            this.monitorDb = monitorDb;    
        }
        
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (messageContext.customData != this) {
                return base.ExecuteTask(task, database, response, messageContext);
            }
            switch (task.TaskType) {
                case TaskType.message:
                    var message = (SendMessage)task;
                    if (message.name == MonitorStore.ClearStats) {
                        monitorDb.authenticator.ClearUserStats();
                        monitorDb.clientController.ClearClientStats();
                        monitorDb.db.authenticator.ClearUserStats();
                        monitorDb.db.clientController.ClearClientStats();
                    }
                    SyncTaskResult messageResult = new SendMessageResult();
                    return Task.FromResult(messageResult);
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