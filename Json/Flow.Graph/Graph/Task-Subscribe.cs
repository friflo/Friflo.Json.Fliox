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
        internal readonly   List<Change>            changes;
        internal readonly   FilterOperation         filter;
        private  readonly   string                  filterLinq; // use as string identifier of a filter
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeChangesTask<{typeof(T).Name}> (filter: {filterLinq})";
        

        internal SubscribeChangesTask(IEnumerable<Change> changes, FilterOperation filter) {
            this.changes    = changes != null ? changes.ToList() : new List<Change>();
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
    }
    
    public class SubscribeMessagesTask : SyncTask
    {
        internal readonly   List<string>            prefixes;
        internal            TaskState               state;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeMessagesTask";
        

        internal SubscribeMessagesTask(IEnumerable<string> prefixes) {
            this.prefixes = prefixes.ToList();
        }
    }
}