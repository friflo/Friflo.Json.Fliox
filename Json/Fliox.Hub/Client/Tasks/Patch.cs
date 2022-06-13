// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PatchTask<T> : SyncTask where T : class
    {
        public              IReadOnlyList<EntityPatchInfo<T>>   Patches => patches;
        
        internal readonly   MemberSelection<T>                  selection;
        [DebuggerBrowsable(Never)]
        internal readonly   List<EntityPatchInfo<T>>            patches;
        private  readonly   SyncSetBase<T>                      syncSet;

        [DebuggerBrowsable(Never)]
        internal            TaskState                           state;
        internal override   TaskState                           State   => state;
        public   override   string                              Details => GetDetails();
        
        internal PatchTask(SyncSetBase<T> syncSet, MemberSelection<T> selection) {
            patches         = new List<EntityPatchInfo<T>>();
            this.syncSet    = syncSet;
            this.selection  = selection;
        }

        public PatchTask<T> Add(T entity) {
            var entities = new List<T> { entity };
            syncSet.AddEntityPatches(this, entities);
            return this;
        }
        
        public PatchTask<T> AddRange(ICollection<T> entities) {
            syncSet.AddEntityPatches(this, entities);
            return this;
        }
        
        internal void SetResult(IDictionary<JsonKey, EntityError> errorsPatch) {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                if (errorsPatch.TryGetValue(patch.entityPatch.id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            } else {
                state.Executed = true;
            }
        }
        
        private string GetDetails() { 
            var sb = new StringBuilder();
            sb.Append("PatchTask<");
            sb.Append(typeof(T).Name);
            sb.Append("> patches: ");
            sb.Append(patches.Count);
            sb.Append(", selection: ");
            selection.FormatToString(sb);
            return sb.ToString();
        }
    }
}
