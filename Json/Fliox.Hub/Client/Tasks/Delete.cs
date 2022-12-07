// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class DeleteTask<TKey, T> : SyncTask where T : class
    {
        private  readonly   SyncSet<TKey, T>    syncSet;
        internal readonly   List<TKey>          keys;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState           state;
        internal override   TaskState           State       => state;
        public   override   string              Details     => $"DeleteTask<{typeof(T).Name}> (entities: {keys.Count})";
        internal override   TaskType            TaskType    => TaskType.delete;


        internal DeleteTask(List<TKey> ids, SyncSet<TKey, T> syncSet) {
            this.syncSet    = syncSet;
            this.keys       = ids;
        }

        public void Add(TKey key) {
            keys.Add(key);
        }
        
        public void AddRange(ICollection<TKey> keys) {
            this.keys.AddRange(keys);
        }

        protected internal override void ReUse() {
            keys.Clear();
            state       = default;
            taskName    = null;
            syncSet.set.GetDeleteBuffer().Push(this);
        }

        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return syncSet.DeleteEntities(this);
        }
    }
    
    public sealed class DeleteAllTask<TKey, T> : SyncTask where T : class
    {
        private  readonly   SyncSet<TKey, T>    syncSet;
        [DebuggerBrowsable(Never)]
        internal            TaskState           state;
        internal override   TaskState           State       => state;

        public   override   string              Details     => $"DeleteAllTask<{typeof(T).Name}>";
        internal override   TaskType            TaskType    => TaskType.delete;

        internal DeleteAllTask(SyncSet<TKey, T>  syncSet) {
            this.syncSet = syncSet;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return new DeleteEntities {
                container   = syncSet.set.name,
                all         = true,
                syncTask    = this 
            };
        }
    }

}