// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class SyncStore
    {
        private             Dictionary<string, SyncSet> syncSets;
        
        internal readonly   List<SyncTask>              appTasks            = new List<SyncTask>();
        private  readonly   List<LogTask>               logTasks            = new List<LogTask>();
        
        internal readonly   List<SendMessageTask>       messageTasks        = new List<SendMessageTask>();
        private             int                         messageTasksIndex;
        
        internal readonly   List<SubscribeMessageTask>  subscribeMessage    = new List<SubscribeMessageTask>();
        private             int                         subscribeMessageIndex;
        
        internal            SyncSet                     GetSyncSet(string name) => syncSets[name];

        internal void SetSyncSets(EntityStore store) {
            var setByName = store._intern.setByName;
            syncSets = new Dictionary<string, SyncSet>(setByName.Count);
            foreach (var pair in setByName) {
                string      container   = pair.Key;
                EntitySet   set         = pair.Value;
                syncSets.Add(container, set.SyncSet);
            }
        }

        internal LogTask CreateLog() {
            var logTask = new LogTask();
            logTasks.Add(logTask);
            return logTask;
        }

        internal void LogResults() {
            foreach (var logTask in logTasks) {
                logTask.state.Synced = true;
                logTask.SetResult();
            }
        }
        
        // ----------------------------------- add tasks methods -----------------------------------
        internal void AddTasks(List<DatabaseTask> tasks) {
            Message             (tasks);
            SubscribeMessage    (tasks);
        }
                
        // --- Message
        private void Message(List<DatabaseTask> tasks) {
            foreach (var messageTask in messageTasks) {
                var req = new SendMessage {
                    name  = messageTask.name,
                    value = messageTask.value
                };
                tasks.Add(req);
            }
        }
        
        internal void MessageResult (SendMessage task, TaskResult result) {
            // consider invalid response
            if (messageTasksIndex >= messageTasks.Count)
                return;
            var index = messageTasksIndex++;
            SendMessageTask messageTask = messageTasks[index];
            if (result is TaskErrorResult taskError) {
                messageTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var messageResult = (SendMessageResult)result;
            messageTask.result = messageResult.result.json;
            messageTask.state.Synced = true;
        }
        
        // --- SubscribeMessage
        private void SubscribeMessage(List<DatabaseTask> tasks) {
            foreach (var subscribe in subscribeMessage) {
                var req = new SubscribeMessage{ name = subscribe.name, remove = subscribe.remove };
                tasks.Add(req);
            }
        }
        
        internal void SubscribeMessageResult (SubscribeMessage task, TaskResult result) {
            // consider invalid response
            if (subscribeMessageIndex >= subscribeMessage.Count)
                return;
            var index = subscribeMessageIndex++;
            var subscribeTask = subscribeMessage[index];
            if (result is TaskErrorResult taskError) {
                subscribeTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            subscribeTask.state.Synced = true;
        }
    }
}