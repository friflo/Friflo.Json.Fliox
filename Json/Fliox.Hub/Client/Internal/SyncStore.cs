// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class SyncStore
    {
        internal    IDictionary<string,SyncSet> SyncSets { get; private set; }
        
        internal readonly   List<SyncTask>      appTasks            = new List<SyncTask>();
        
        private     List<DetectPatchesTask>     logTasks;
        private     List<DetectPatchesTask>     LogTasks()          => logTasks ?? (logTasks = new List<DetectPatchesTask>());
        
        internal    List<MessageTask>           messageTasks;
        internal    List<MessageTask>           MessageTasks()      => messageTasks ?? (messageTasks = new List<MessageTask>());
        private     int                         messageTasksIndex;
        
        private     List<SubscribeMessageTask>  subscribeMessage;
        internal    List<SubscribeMessageTask>  SubscribeMessage()  => subscribeMessage ?? (subscribeMessage = new List<SubscribeMessageTask>());
        private     int                         subscribeMessageIndex;
        
        internal void SetSyncSets(FlioxClient store) {
            SyncSets = store._intern.CreateSyncSets();
        }

        internal DetectPatchesTask CreateDetectPatchesTask() {
            var logTask = new DetectPatchesTask();
            LogTasks().Add(logTask);
            return logTask;
        }

        internal void LogResults() {
            if (logTasks == null)
                return;
            foreach (var logTask in logTasks) {
                logTask.state.Executed = true;
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
                SyncMessageTask msg;
                if (messageTask is CommandTask) {
                    msg = new SendCommand { name  = messageTask.name, param = messageTask.param };
                } else {
                    msg = new SendMessage { name  = messageTask.name, param = messageTask.param };
                }
                tasks.Add(msg);
            }
        }
        
        internal void MessageResult (SyncMessageTask task, SyncTaskResult result) {
            // consider invalid response
            if (messageTasks == null || messageTasksIndex >= messageTasks.Count)
                return;
            var index = messageTasksIndex++;
            var messageTask = messageTasks[index];
            if (result is TaskErrorResult taskError) {
                messageTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            if (messageTask is CommandTask cmd) {
                var messageResult = (SendCommandResult)result;
                cmd.result = messageResult.result;
            }
            messageTask.state.Executed = true;
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
            subscribeTask.state.Executed = true;
        }
    }
}