// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    public class SubscribeChangesTask<T> : SyncTask where T : Entity
    {
        internal            TaskState               state;
        internal readonly   HashSet<Change>         changes;
        internal readonly   FilterOperation         filter;
        private  readonly   string                  filterLinq; // use as string identifier of a filter
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeTask<{typeof(T).Name}> (filter: {filterLinq})";
        

        internal SubscribeChangesTask(HashSet<Change> changes, FilterOperation filter) {
            this.changes    = changes ?? new HashSet<Change>();
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
    }
    
    public class SubscribeEchosTask : SyncTask
    {
        internal readonly   List<string>            prefixes;
        internal            TaskState               state;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeEchoTask";
        

        internal SubscribeEchosTask(IEnumerable<string> prefixes) {
            this.prefixes = prefixes.ToList();
        }
    }
}