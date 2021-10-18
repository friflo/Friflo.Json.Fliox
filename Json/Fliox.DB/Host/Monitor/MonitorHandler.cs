// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;


namespace Friflo.Json.Fliox.DB.Host.Monitor
{
    public class MonitorHandler : TaskHandler
    {
        internal MonitorHandler() { }
        
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var monitorDb = (MonitorDatabase)database;
            switch (task.TaskType) {
                case TaskType.message:
                    var message = (SendMessage)task;
                    if (message.name == MonitorStore.ClearStats) {
                        // clear request counts of default database. They contain also request counts of all extension databases.
                        var baseDB = monitorDb.extensionBase;
                        baseDB.Authenticator.ClearUserStats();
                        baseDB.ClientController.ClearClientStats();
                        baseDB.hostStats.ClearHostStats();
                        SyncTaskResult messageResult = new SendMessageResult();
                        return Task.FromResult(messageResult);
                    }
                    return base.ExecuteTask(task, monitorDb.stateDB, response, messageContext);
                case TaskType.read:
                case TaskType.query:
                    return base.ExecuteTask(task, monitorDb.stateDB, response, messageContext);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"MonitorDatabase does not support task: '{task.TaskType}'");
                    return Task.FromResult(result);
            }
        }
    }
}