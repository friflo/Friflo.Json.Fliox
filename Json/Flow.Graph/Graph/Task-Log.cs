// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public class LogTask : SyncTask
    {
        private  readonly   List<EntityPatch>   patches = new List<EntityPatch>();
        private  readonly   HashSet<string>     creates = new HashSet<string>();
        
        public              int                 Patches   => patches.Count;
        public              int                 Creates   => creates.Count;

        internal            TaskState           state;
        internal override   TaskState           State   => state;
        
        internal override   string              Label   => $"LogTask patches: {patches.Count}, creates: {creates.Count}";
        public   override   string              ToString()  => Label;

        internal LogTask() { }

        internal void AddPatch(EntityPatch patch) {
            if (patch == null)
                throw new ArgumentException("patch must not be null");
            patches.Add(patch);
        }
        
        internal void AddCreate(string id) {
            if (id == null)
                throw new ArgumentException("id must not be null");
            creates.Add(id);
        }

        internal void SetResult() {
            foreach (var patch in patches) {
                if (patch.taskError != null) {
                    state.SetError(new TaskErrorInfo(patch.taskError));
                }
            }
        }
    }
    
}