// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Client.Internal
{
    internal sealed class SyncStore
    {
        internal            Dictionary<string, SyncSet> SyncSets { get; private set; }
        
        internal readonly   List<SyncTask>      appTasks            = new List<SyncTask>();
        
        private     List<LogTask>               logTasks;
        private     List<LogTask>               LogTasks()          => logTasks ?? (logTasks = new List<LogTask>());
        
        internal    List<SendMessageTask>       messageTasks;
        internal    List<SendMessageTask>       MessageTasks()      => messageTasks ?? (messageTasks = new List<SendMessageTask>());
        private     int                         messageTasksIndex;
        
        private     List<SubscribeMessageTask>  subscribeMessage;
        internal    List<SubscribeMessageTask>  SubscribeMessage()  => subscribeMessage ?? (subscribeMessage = new List<SubscribeMessageTask>());
        private     int                         subscribeMessageIndex;
        
        internal void SetSyncSets(EntityStore store) {
            var setByName = store._intern.setByName;
            SyncSets = new Dictionary<string, SyncSet>(setByName.Count);
            foreach (var pair in setByName) {
                string      container   = pair.Key;
                EntitySet   set         = pair.Value;
                SyncSet     syncSet     = set.SyncSet;
                if (syncSet != null) {
                    SyncSets.Add(container, set.SyncSet);
                }
            }
        }

        internal LogTask CreateLog() {
            var logTask = new LogTask();
            LogTasks().Add(logTask);
            return logTask;
        }

        internal void LogResults() {
            if (logTasks == null)
                return;
            foreach (var logTask in logTasks) {
                logTask.state.Synced = true;
                logTask.SetResult();
            }
        }
        
        // ----------------------------------- add tasks methods -----------------------------------
        internal void AddTasks(List<SyncRequestTask> tasks) {
            Message             (tasks);
            SubscribeMessage    (tasks);
        }
                
        // --- Message
        private void Message(List<SyncRequestTask> tasks) {
            if (messageTasks == null)
                return;
            foreach (var messageTask in messageTasks) {
                var req = new SendMessage {
                    name  = messageTask.name,
                    value = messageTask.value
                };
                tasks.Add(req);
            }
        }
        
        internal void MessageResult (SendMessage task, SyncTaskResult result) {
            // consider invalid response
            if (messageTasks == null || messageTasksIndex >= messageTasks.Count)
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
        private void SubscribeMessage(List<SyncRequestTask> tasks) {
            if (subscribeMessage == null)
                return;
            foreach (var subscribe in subscribeMessage) {
                var req = new SubscribeMessage{ name = subscribe.name, remove = subscribe.remove };
                tasks.Add(req);
            }
        }
        
        internal void SubscribeMessageResult (SubscribeMessage task, SyncTaskResult result) {
            // consider invalid response
            if (subscribeMessage == null || subscribeMessageIndex >= subscribeMessage.Count)
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