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
        private  readonly   List<Change>        patches = new List<Change>();
        private  readonly   List<Change>        creates = new List<Change>();
        
        public              int                 Patches   => patches.Count;
        public              int                 Creates   => creates.Count;

        internal            TaskState           state;
        internal override   TaskState           State   => state;
        
        internal override   string              Label   => $"LogTask patches: {patches.Count}, creates: {creates.Count}";
        public   override   string              ToString()  => Label;

        internal LogTask() { }

        internal void AddPatch(SyncSet sync, string id) {
            if (id == null)
                throw new ArgumentException("id must not be null");
            var change = new Change(sync, id);
            patches.Add(change);
        }
        
        internal void AddCreate(SyncSet sync, string id) {
            if (id == null)
                throw new ArgumentException("id must not be null");
            var change = new Change(sync, id);
            creates.Add(change);
        }

        internal void SetResult() {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                /* if (patch.taskError != null) {
                    state.SetError(new TaskErrorInfo(patch.taskError));
                }*/
            }
            foreach (var create in creates) {
                if (create.sync.createErrors.TryGetValue(create.id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            }
        }
    }
    
    /// Identify entries in <see cref="SyncSet{T}.patches"/> or <see cref="SyncSet{T}.creates"/> by tuple
    /// <see cref="sync"/> and <see cref="id"/>
    internal readonly struct Change {
        internal readonly SyncSet sync;
        internal readonly string  id;

        internal Change(SyncSet sync, string id) {
            this.sync   = sync;
            this.id     = id;
        }
    }
    
}