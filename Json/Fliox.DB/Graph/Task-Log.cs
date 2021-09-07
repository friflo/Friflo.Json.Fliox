// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Graph.Internal;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph
{
    public class LogTask : SyncTask
    {
        private  readonly   List<LogChange>     patches = new List<LogChange>();
        private  readonly   List<LogChange>     creates = new List<LogChange>();
        
        public              int                 GetPatchCount()   => patches.Count; // count as method to avoid flooding properties
        public              int                 GetCreateCount()  => creates.Count; // count as method to avoid flooding properties

        internal            TaskState           state;
        internal override   TaskState           State   => state;
        
        public   override   string              Details   => $"LogTask (patches: {patches.Count}, creates: {creates.Count})";
        

        internal LogTask() { }

        /// <summary>Log entity patch previously added to <see cref="SyncSet{TKey,T}.Patches"/></summary>
        internal void AddPatch(SyncSet sync, in JsonKey id) {
            if (id.IsNull())
                throw new ArgumentException("id must not be null");
            var change = new LogChange(sync, id);
            patches.Add(change);
        }
        
        /// <summary>Log created entity previously added to <see cref="SyncSet{TKey,T}.Creates"/></summary>
        internal void AddCreate(SyncSet sync, in JsonKey id) {
            if (id.IsNull())
                throw new ArgumentException("id must not be null");
            var change = new LogChange(sync, id);
            creates.Add(change);
        }

        internal void SetResult() {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                if (patch.sync.errorsPatch.TryGetValue(patch.id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            foreach (var create in creates) {
                if (create.sync.errorsCreate.TryGetValue(create.id, out EntityError error)) {
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
        internal readonly   SyncSet     sync;
        internal readonly   JsonKey     id;

        internal LogChange(SyncSet sync, in JsonKey id) {
            this.sync   = sync;
            this.id     = id;
        }
    }
    
}