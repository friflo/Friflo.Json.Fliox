// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
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
        private  readonly   List<TKey>          keys;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState           state;
        internal override   TaskState           State       => state;

        public   override   string              Details     => $"DeleteTask<{typeof(T).Name}> (#keys: {keys.Count})";
        
        
        internal DeleteTask(List<TKey> ids, SyncSet<TKey, T> syncSet) {
            this.syncSet    = syncSet;
            this.keys       = ids;
        }

        public void Add(TKey key) {
            syncSet.AddDelete(key);
            keys.Add(key);
        }
        
        public void AddRange(ICollection<TKey> keys) {
            syncSet.AddDeleteRange(keys);
            this.keys.AddRange(keys);
        }

        internal void GetKeys(List<TKey> keys) {
            keys.AddRange(this.keys);
        }
    }
    
    public sealed class DeleteAllTask<TKey, T> : SyncTask where T : class
    {
        [DebuggerBrowsable(Never)]
        internal            TaskState           state;
        internal override   TaskState           State       => state;

        public   override   string              Details     => $"DeleteAllTask<{typeof(T).Name}>";

        internal DeleteAllTask() {
        }
    }

}