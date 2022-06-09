// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class DetectAllPatchesTask : SyncTask
    {
        public              IReadOnlyList<DetectPatchesTask>    ContainerPatches => entitySetPatches;
        
        internal readonly   List<DetectPatchesTask>             entitySetPatches = new List<DetectPatchesTask>();
        
        [DebuggerBrowsable(Never)]
        private             TaskState                           state;
        internal override   TaskState                           State   => state;
        
        public   override   string                              Details   => $"DetectAllPatchesTask (patches: {GetPatchCount()})";

        internal DetectAllPatchesTask() { }

        internal void SetResult() {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patchesTask in entitySetPatches) {
                var syncSet = patchesTask.syncSet;
                foreach (var patch in patchesTask.patches) {
                    if (syncSet.errorsPatch.TryGetValue(patch.entityPatch.id, out EntityError error)) {
                        entityErrorInfo.AddEntityError(error);
                    }
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            } else {
                state.Executed = true;
            }
        }
        
        // count as method to avoid flooding properties
        public int GetPatchCount() {
            int result = 0;
            foreach (var patchesTask in entitySetPatches) {
                result += patchesTask.patches.Count;
            }
            return result;
        } 
    }
}