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
    public class DeleteTask<T> : SyncTask where T : Entity
    {
        private  readonly   EntitySet<T>        set;
        private  readonly   List<string>        ids;
        internal            TaskState           state;
        internal override   TaskState           State       => state;

        public   override   string              Details     => $"DeleteTask<{typeof(T).Name}> (#ids: {ids.Count})";
        
        
        internal DeleteTask(List<string> ids, EntitySet<T> set) {
            this.set = set;
            this.ids = ids;
        }

        public void Add(string id) {
            set.sync.AddDelete(id);
            ids.Add(id);
        }
        
        public void AddRange(ICollection<string> ids) {
            foreach (var id in ids) {
                set.sync.AddDelete(id);
            }
            this.ids.AddRange(ids);
        }

        internal void GetIds(List<string> ids) {
            ids.AddRange(this.ids);
        }
    }
}