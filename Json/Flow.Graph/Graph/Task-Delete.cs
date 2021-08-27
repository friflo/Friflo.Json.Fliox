// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DeleteTask<TKey, T> : SyncTask where T : class
    {
        private  readonly   EntitySet<TKey, T>  set;
        private  readonly   List<TKey>          keys;
        internal            TaskState           state;
        internal override   TaskState           State       => state;

        public   override   string              Details     => $"DeleteTask<{typeof(T).Name}> (#keys: {keys.Count})";
        
        
        internal DeleteTask(List<TKey> ids, EntitySet<TKey, T> set) {
            this.set = set;
            this.keys = ids;
        }

        public void Add(TKey key) {
            set.syncSet.AddDelete(key);
            keys.Add(key);
        }
        
        public void AddRange(ICollection<TKey> keys) {
            set.syncSet.AddDeleteRange(keys);
            this.keys.AddRange(keys);
        }

        internal void GetKeys(List<TKey> keys) {
            keys.AddRange(this.keys);
        }
    }
}