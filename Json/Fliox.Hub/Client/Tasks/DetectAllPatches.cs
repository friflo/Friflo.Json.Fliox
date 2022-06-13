// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class DetectAllPatches
    {
        public              IReadOnlyList<DetectPatchesTask>    EntitySetPatches    => entitySetPatches;
        public              int                                 PatchCount          => GetPatchCount();
        public              bool                                Success             => GetSuccess();
        
        [DebuggerBrowsable(Never)] private  bool                                isExecuted;
        [DebuggerBrowsable(Never)] private  bool                                success;
        [DebuggerBrowsable(Never)] internal readonly List<DetectPatchesTask>    entitySetPatches = new List<DetectPatchesTask>();
        
        public   override   string                              ToString() => $"DetectAllPatchesTask (patches: {GetPatchCount()})";

        internal DetectAllPatches() { }
        
        public DetectPatchesTask<T> GetPatches<TKey,T>(EntitySet<TKey,T> entitySet) where T : class {
            foreach (var detectPatchesTask in entitySetPatches) {
                if (detectPatchesTask.Container != entitySet.name)
                    continue;
                return (DetectPatchesTask<T>)detectPatchesTask;
            }
            return null;
        }
        
        private bool GetSuccess() {
            if (!isExecuted)    throw new TaskNotSyncedException("DetectAllPatchesTask");
            return success;
        }

        internal void SetResult() {
            isExecuted = true;
            success = true;
            foreach (var patchesTask in entitySetPatches) {
                if (patchesTask.Success)
                    continue;
                success = false;
                return;
            }
        }
        
        private int GetPatchCount() {
            int result = 0;
            foreach (var patchesTask in entitySetPatches) {
                result += patchesTask.GetPatchCount();
            }
            return result;
        } 
    }
}