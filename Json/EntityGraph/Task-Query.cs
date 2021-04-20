// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Internal;
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryTask -----------------------------------------
    public class QueryTask<T> where T : Entity
    {
        internal readonly   FilterOperation filter;
        private  readonly   EntitySet<T>    set;
        internal            bool            synced;
        internal readonly   List<T>         entities = new List<T>();
        
        public              List<T>         Result          => synced ? entities        : throw Error("QueryTask.Result requires Sync().");
        public              T               this[int index] => synced ? entities[index] : throw Error("QueryTask[] requires Sync().");

        public override     string          ToString() => filter.Linq;

        internal QueryTask(FilterOperation filter, EntitySet<T> set) {
            this.filter = filter;
            this.set    = set;
        }
        
        private Exception Error(string message) {
            return new PeerNotSyncedException($"{message} Entity: {set.name} filter: {filter.Linq}");
        }
    }
}

