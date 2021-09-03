// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Db.Graph.Internal;
using Friflo.Json.Fliox.Db.Sync;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Db.Graph
{
    public class SubscribeChangesTask<T> : SyncTask where T : class
    {
        internal            TaskState               state;
        internal            List<Change>            changes;
        internal            FilterOperation         filter;
        private             string                  filterLinq; // use as string identifier of a filter
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeChangesTask<{typeof(T).Name}> (filter: {filterLinq})";
        
        internal  SubscribeChangesTask() { }
            
        internal void Set(IEnumerable<Change> changes, FilterOperation filter) {
            this.changes    = changes != null ? changes.ToList() : new List<Change>();
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
    }
    
    public class SubscribeMessageTask : SyncTask
    {
        internal readonly   string                  name;
        internal readonly   bool?                   remove;
        internal            TaskState               state;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeMessageTask (name: {name})";
        

        internal SubscribeMessageTask(string name, bool? remove) {
            this.name   = name;
            this.remove = remove;
        }
    }
}