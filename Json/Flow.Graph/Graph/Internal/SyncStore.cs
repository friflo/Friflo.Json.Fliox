// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class SyncStore
    {
        internal readonly   List<SyncTask>                  appTasks            = new List<SyncTask>();
        private  readonly   List<LogTask>                   logTasks            = new List<LogTask>();
        internal readonly   Dictionary<string, MessageTask> messageTasks        = new Dictionary<string, MessageTask>();
        
        internal readonly   List<SubscribeMessagesTask>     subscribeMessages   = new List<SubscribeMessagesTask>();
        internal            int                             subscribeMessagesIndex;

        
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
            Message            (tasks);
            SubscribeMessages  (tasks);
        }
                
        // --- Message
        private void Message(List<DatabaseTask> tasks) {
            foreach (var entry in messageTasks) {
                MessageTask messageTask = entry.Value;
                var req = new Message {
                    name  = messageTask.name,
                    value = messageTask.value
                };
                tasks.Add(req);
            }
        }
        
        internal void MessageResult (Message task, TaskResult result) {
            MessageTask messageTask = messageTasks[task.name];
            if (result is TaskErrorResult taskError) {
                messageTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var messageResult = (MessageResult)result;
            messageTask.result = messageResult.name;
            messageTask.state.Synced = true;
        }
        
        // --- SubscribeMessages
        private void SubscribeMessages(List<DatabaseTask> tasks) {
            foreach (var subscribe in subscribeMessages) {
                var req = new SubscribeMessages{ tags = subscribe.tags};
                tasks.Add(req);
            }
        }
        
        internal void SubscribeMessagesResult (SubscribeMessages task, TaskResult result) {
            // consider invalid response
            if (subscribeMessagesIndex >= subscribeMessages.Count)
                return;
            var index = subscribeMessagesIndex++;
            var subscribeTask = subscribeMessages[index];
            if (result is TaskErrorResult taskError) {
                subscribeTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            subscribeTask.state.Synced = true;
        }
    }
}