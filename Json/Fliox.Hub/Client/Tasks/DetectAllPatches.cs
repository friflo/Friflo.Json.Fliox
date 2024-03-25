// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// <see cref="DetectAllPatches"/> is a container of <see cref="DetectPatchesTask"/>'s. <br/>
    /// It is returned by <see cref="FlioxClient.DetectAllPatches"/> and contain <see cref="EntitySetPatches"/> for
    /// all <see cref="EntitySet{TKey,T}"/>'s where entity patches are found.
    /// </summary>
    public sealed class DetectAllPatches
    {
        /// <summary>List of detected <see cref="DetectPatchesTask"/>'s per <see cref="EntitySet{TKey,T}"/></summary>
        public              IReadOnlyList<DetectPatchesTask>    EntitySetPatches    => entitySetPatches;
        /// <summary>Number of all detected entity patches</summary>
        public              int                                 PatchCount          => GetPatchCount();
        /// <summary>Is true in case all <see cref="EntitySetPatches"/> applied successful</summary>
        public              bool                                Success             => GetSuccess();
        
        [DebuggerBrowsable(Never)] private  bool                                isExecuted;
        [DebuggerBrowsable(Never)] private  bool                                success;
        [DebuggerBrowsable(Never)] internal readonly List<DetectPatchesTask>    entitySetPatches = new List<DetectPatchesTask>();
        
        public   override   string                              ToString() => $"DetectAllPatchesTask (patches: {GetPatchCount()})";

        internal DetectAllPatches() { }
        
        /// <summary>return type-safe patches of the given <paramref name="entitySet"/></summary>
        public DetectPatchesTask<TKey,T> GetPatches<TKey,T>(EntitySet<TKey,T> entitySet) where T : class {
            var set = entitySet.GetInstance();
            foreach (var detectPatchesTask in entitySetPatches) {
                if (detectPatchesTask.Container != set.name)
                    continue;
                return (DetectPatchesTask<TKey,T>)detectPatchesTask;
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