// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class SyncStore
    {
        internal    Dictionary<ShortString,SyncSet> SyncSets { get; private set; }
        
        internal readonly   List<SyncFunction>  functions           = new List<SyncFunction>();
        
        private     List<DetectAllPatches>      detectAllPatches;
        private     List<DetectAllPatches>      DetectAllPatches()  => detectAllPatches ??= new List<DetectAllPatches>();
        
        internal void SetSyncSets(FlioxClient store) {
            SyncSets = CreateSyncSets(store, SyncSets);
        }
        
        private static Dictionary<ShortString, SyncSet> CreateSyncSets(FlioxClient client, Dictionary<ShortString,SyncSet> syncSets) {
            var count = 0;
            syncSets?.Clear();
            var sets =  client.entitySets;
            foreach (var set in sets) {
                SyncSet syncSet = set?.SyncSet;
                if (syncSet == null)
                    continue;
                count++;
            }
            if (count == 0) {
                return syncSets;
            }
            // create Dictionary<,> only if required
            syncSets = syncSets ?? new Dictionary<ShortString, SyncSet>(count, ShortString.Equality);
            foreach (var set in sets) {
                SyncSet syncSet = set?.SyncSet;
                if (syncSet == null)
                    continue;
                syncSets.Add(set.nameShort, syncSet);
            }
            return syncSets;
        }
        
        internal void Reuse() {
            foreach (var function in functions) {
                function.Reuse();
            }
            var syncSets = SyncSets;
            if (syncSets != null) {
                foreach (var syncSet in syncSets) {
                    syncSet.Value.Reuse();
                }
                syncSets.Clear();
            }
            detectAllPatches?.Clear();
            functions.Clear();
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