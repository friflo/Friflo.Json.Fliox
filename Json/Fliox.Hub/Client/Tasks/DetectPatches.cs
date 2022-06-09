// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class DetectPatchesTask : SyncTask
    {
        private  readonly   List<LogChange>     patches = new List<LogChange>();
        
        public              int                 GetPatchCount()   => patches.Count; // count as method to avoid flooding properties
        
        [DebuggerBrowsable(Never)]
        internal            TaskState           state;
        internal override   TaskState           State   => state;
        
        public   override   string              Details   => $"DetectPatches (patches: {patches.Count})";
        

        internal DetectPatchesTask() { }

        /// <summary>Log entity patch previously added to <see cref="SyncSet{TKey,T}.Patches"/></summary>
        internal void AddPatch(SyncSet sync, EntityPatch entityPatch) {
            if (entityPatch.id.IsNull())
                throw new ArgumentException("id must not be null");
            var patch = new LogChange(sync, entityPatch);
            patches.Add(patch);
        }
        
        internal void SetResult() {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                if (patch.sync.errorsPatch.TryGetValue(patch.entityPatch.id, out EntityError error)) {
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
    internal readonly struct LogChange {
        internal    readonly    EntityPatch     entityPatch;
        internal    readonly    SyncSet         sync;

        internal LogChange(SyncSet sync, EntityPatch     entityPatch) {
            this.entityPatch    = entityPatch;
            this.sync           = sync;
        }
    }
}
