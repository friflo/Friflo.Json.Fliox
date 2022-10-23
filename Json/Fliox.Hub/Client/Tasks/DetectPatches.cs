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
    public abstract class DetectPatchesTask : SyncTask
    {
        public   abstract   string                          Container   { get; }
        public   abstract   IReadOnlyList<EntityPatchInfo>  GetPatches();
        internal abstract   int                             GetPatchCount();
    }
    
    public sealed class DetectPatchesTask<TKey,T> : DetectPatchesTask  where T : class
    {
        public              IReadOnlyList<EntityPatchInfo<T>>   Patches     => patches;
        [DebuggerBrowsable(Never)]
        private  readonly   List<EntityPatchInfo<T>>            patches;
        internal readonly   Dictionary<JsonKey, EntityPatch>    entityPatches;
        private  readonly   SyncSet<TKey,T>                     syncSet;

        [DebuggerBrowsable(Never)]
        internal            TaskState                           state;
        internal override   TaskState                           State   => state;
        public   override   string                              Details => $"DetectPatchesTask (container: {Container}, patches: {patches.Count})";
        internal override   TaskType                            TaskType=> TaskType.patch;
        
        public   override   string                              Container       => syncSet.EntitySet.name;
        internal override   int                                 GetPatchCount() => patches.Count;

        internal DetectPatchesTask(SyncSet<TKey,T> syncSet) {
            this.syncSet    = syncSet;
            patches         = new List<EntityPatchInfo<T>>();
            entityPatches   = new Dictionary<JsonKey, EntityPatch>(JsonKey.Equality);
        }
        
        public override IReadOnlyList<EntityPatchInfo> GetPatches() {
            var result = new List<EntityPatchInfo>(patches.Count);
            foreach (var patch in patches) {
                result.Add(new EntityPatchInfo(patch.entityPatch));
            }
            return result;
        }

        internal void AddPatch(EntityPatch entityPatch, T entity) {
            if (entityPatch.id.IsNull())
                throw new ArgumentException("id must not be null");
            var patch = new EntityPatchInfo<T>(entityPatch, entity);
            patches.Add(patch);
        }
        
        internal void SetResult() {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                if (syncSet.errorsPatch.TryGetValue(patch.entityPatch.id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            } else {
                state.Executed = true;
            }
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return syncSet.PatchEntities(this);
        }
    }
}
