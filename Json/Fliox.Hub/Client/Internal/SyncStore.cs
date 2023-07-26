// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class SyncStore
    {
        internal readonly   List<SyncTask>                  tasks               = new List<SyncTask>();
        
        private             List<DetectAllPatches>          detectAllPatches;
        private             List<DetectAllPatches>          DetectAllPatches()  => detectAllPatches ??= new List<DetectAllPatches>();
        
        internal void Reuse(int entitySetCount) {
            Span<bool> reusedSyncSets = stackalloc bool[entitySetCount];
            foreach (var task in tasks) {
                task.Reuse();
                var syncSet = task.taskSyncSet;
                if (syncSet == null) {
                    continue;
                }
                var index = syncSet.EntitySet.index;
                // Ensure SyncSet is reused only once
                if (reusedSyncSets[index]) {
                    continue;
                }
                reusedSyncSets[index] = true;
                syncSet.Reuse();
            }
            detectAllPatches?.Clear();
            tasks.Clear();
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
        // --- Message
        internal static void MessageResult (SyncMessageTask task, SyncTaskResult result) {
            var messageTask = (MessageTask)task.intern.syncTask;
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
        internal static void SubscribeMessageResult (SubscribeMessage task, SyncTaskResult result) {
            var subscribeTask = (SubscribeMessageTask)task.intern.syncTask;
            if (result is TaskErrorResult taskError) {
                subscribeTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            subscribeTask.state.Executed = true;
        }
    }
}