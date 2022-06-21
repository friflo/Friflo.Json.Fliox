// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class SyncStore
    {
        internal    IDictionary<string,SyncSet> SyncSets { get; private set; }
        
        internal readonly   List<SyncFunction>  functions           = new List<SyncFunction>();
        
        private     List<DetectAllPatches>      detectAllPatches;
        private     List<DetectAllPatches>      DetectAllPatches()  => detectAllPatches ?? (detectAllPatches = new List<DetectAllPatches>());
        
        private     List<SubscribeMessageTask>  subscribeMessage;
        internal    List<SubscribeMessageTask>  SubscribeMessage()  => subscribeMessage ?? (subscribeMessage = new List<SubscribeMessageTask>());
        private     int                         subscribeMessageIndex;
        
        internal void SetSyncSets(FlioxClient store) {
            SyncSets = store._intern.CreateSyncSets();
        }

        internal DetectAllPatches CreateDetectAllPatchesTask() {
            var task = new DetectAllPatches();
            DetectAllPatches().Add(task);
            return task;
        }

        internal void DetectPatchesResults() {
            if (detectAllPatches == null)
                return;
            foreach (var logTask in detectAllPatches) {
                logTask.SetResult();
            }
        }
        
        // ----------------------------------- add tasks methods -----------------------------------
        internal void AddTasks(List<SyncRequestTask> tasks) {
        //  Message             (tasks);
            SubscribeMessage    (tasks);
        }
                
        // --- Message
        internal void MessageResult (SyncMessageTask task, SyncTaskResult result) {
            var messageTask = (MessageTask)task.syncTask;
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