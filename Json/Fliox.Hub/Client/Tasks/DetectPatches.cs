// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
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

        internal DetectPatchesTask(Set set) : base(set) { }
    }
    
    public sealed class DetectPatchesTask<TKey,T> : DetectPatchesTask  where T : class
    {
        public              IReadOnlyList<EntityPatchInfo<TKey,T>>  Patches     => patches;
        [DebuggerBrowsable(Never)]
        internal readonly   List<EntityPatchInfo<TKey,T>>           patches;
        private  readonly   Set<TKey,T> set;

        [DebuggerBrowsable(Never)]
        internal            TaskState   state;
        internal override   TaskState   State   => state;
        public   override   string      Details => $"DetectPatchesTask (container: {Container}, patches: {patches.Count})";
        internal override   TaskType    TaskType=> TaskType.merge;
        
        public   override   string      Container       => set.name;
        internal override   int         GetPatchCount() => patches.Count;
        
        private static readonly KeyConverter<TKey>  KeyConvert      = KeyConverter.GetConverter<TKey>();

        internal DetectPatchesTask(Set<TKey,T> set) : base(set) {
            this.set    = set;
            patches     = new List<EntityPatchInfo<TKey,T>>();
        }
        
        public override IReadOnlyList<EntityPatchInfo> GetPatches() {
            var result = new List<EntityPatchInfo>(patches.Count);
            foreach (var patch in patches) {
                var id = KeyConvert.KeyToId(patch.Key);
                result.Add(new EntityPatchInfo(id, patch.entityPatch));
            }
            return result;
        }

        internal void AddPatch(in JsonValue mergePatch, TKey key, T entity) {
            var patch = new EntityPatchInfo<TKey,T>(mergePatch, key, entity);
            patches.Add(patch);
        }
        
        internal void SetResult(IDictionary<JsonKey, EntityError> patchErrors) {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                var id = KeyConvert.KeyToId(patch.Key);
                if (patchErrors.TryGetValue(id, out EntityError error)) {
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
            return set.MergeEntities(this);
        }
    }
}
