// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryTask -----------------------------------------
    public class QueryTask<T> : SubRefsBase<T> where T : Entity
    {
        internal readonly   FilterOperation     filter;
        internal readonly   string              filterLinq; // use as string identifier of a filter 
        internal readonly   List<T>             entities = new List<T>();

        public              List<T>             Result          => synced ? entities        : throw RequiresSyncError("QueryTask.Result requires Sync().");
        public              T                   this[int index] => synced ? entities[index] : throw RequiresSyncError("QueryTask[] requires Sync().");
                     
        public override     string              ToString() => filterLinq;

        internal QueryTask(FilterOperation filter, EntitySet<T> set) : base (filter.Linq, set){
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
        
        private Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} Entity: {set.name} filter: {filterLinq}");
        }

    }
}

