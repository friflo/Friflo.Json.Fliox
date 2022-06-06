// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Transform;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class DetectPatchesTask : SyncTask
    {
        private  readonly   List<LogChange>     patches = new List<LogChange>();
        
        public              int                 GetPatchCount()   => patches.Count; // count as method to avoid flooding properties
        
        internal            TaskState           state;
        internal override   TaskState           State   => state;
        
        public   override   string              Details   => $"DetectPatches (patches: {patches.Count})";
        

        internal DetectPatchesTask() { }

        /// <summary>Log entity patch previously added to <see cref="SyncSet{TKey,T}.Patches"/></summary>
        internal void AddPatch(SyncSet sync, in JsonKey id, List<JsonPatch> patchList) {
            if (id.IsNull())
                throw new ArgumentException("id must not be null");
            var change = new LogChange(sync, id, patchList);
            patches.Add(change);
        }
        
        internal void SetResult() {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                if (patch.sync.errorsPatch.TryGetValue(patch.id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            }
        }
    }
    
    /// Identify entries in <see cref="SyncSet{TKey,T}.Patches"/> and <see cref="SyncSet{TKey,T}.Creates"/> by
    /// tuple <see cref="sync"/> and <see cref="id"/>
    public readonly struct LogChange {
        public      readonly    JsonKey         id;
        public      readonly    List<JsonPatch> patchList;
        internal    readonly    SyncSet         sync;

        internal LogChange(SyncSet sync, in JsonKey id, List<JsonPatch> patchList) {
            this.sync       = sync;
            this.id         = id;
            this.patchList  = patchList;
        }
    }
    
}