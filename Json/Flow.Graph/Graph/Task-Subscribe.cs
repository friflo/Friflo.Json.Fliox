// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    public class SubscribeTask<T> : SyncTask where T : Entity
    {
        internal            TaskState               state;
        internal readonly   TaskType[]              types;
        internal readonly   FilterOperation         filter;
        private  readonly   string                  filterLinq; // use as string identifier of a filter 
     // private  readonly   EntityStore             store;

        // public              Dictionary<string, T>   Results         => IsOk("QueryTask.Result",  out Exception e) ? entities     : throw e;
        // public              T                       this[string id] => IsOk("QueryTask[]",       out Exception e) ? entities[id] : throw e;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeTask<{typeof(T).Name}> (filter: {filterLinq})";
        

        internal SubscribeTask(TaskType[] types, FilterOperation filter) {
            this.types      = types;
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            // this.store      = store;
        }
    }
}