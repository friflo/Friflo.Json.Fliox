// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class SyncStore
    {
        internal readonly   List<SyncTask>                  appTasks        = new List<SyncTask>();
        private  readonly   List<LogTask>                   logTasks        = new List<LogTask>();
        internal readonly   Dictionary<string, MessageTask> messageTasks    = new Dictionary<string, MessageTask>();
        internal            SubscribeMessagesTask           subscribeMessages;

        
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
                    text   = messageTask.text,
                };
                tasks.Add(req);
            }
        }
        
        internal void MessageResult (Message task, TaskResult result) {
            MessageTask messageTask = messageTasks[task.text];
            if (result is TaskErrorResult taskError) {
                messageTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var messageResult = (MessageResult)result;
            messageTask.result = messageResult.text;
            messageTask.state.Synced = true;
        }
        
        // --- SubscribeMessages
        private void SubscribeMessages(List<DatabaseTask> tasks) {
            if (subscribeMessages == null)
                return;
            var req = new SubscribeMessages{ prefixes = subscribeMessages.prefixes};
            tasks.Add(req);
        }
        
        internal void SubscribeMessagesResult (SubscribeMessages task, TaskResult result) {
            if (result is TaskErrorResult taskError) {
                subscribeMessages.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            subscribeMessages.state.Synced = true;
        }
    }
}