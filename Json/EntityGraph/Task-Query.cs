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
        internal readonly   FilterOperation     filter;
        internal readonly   string              filterLinq; // use as string identifier of a filter 
        internal            bool                synced;
        internal readonly   List<T>             entities = new List<T>();
        internal            SubRefs             subRefs;

        
        public              List<T>             Result          => synced ? entities        : throw RequiresSyncError("QueryTask.Result requires Sync().");
        public              T                   this[int index] => synced ? entities[index] : throw RequiresSyncError("QueryTask[] requires Sync().");
                     
        public override     string              ToString() => filterLinq;

        internal QueryTask(FilterOperation filter, EntitySet<T> set) {
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.subRefs    = new SubRefs(filter.Linq, set);
        }
        
        private Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} Entity: {subRefs.set.name} filter: {filterLinq}");
        }
        
        public SubRefsTask<TValue> QueryRefsByPath<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw subRefs.AlreadySyncedError();
            return subRefs.QueryRefsByPathIntern<TValue>(selector);
        }
        
        public SubRefsTask<TValue> QueryRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (synced)
                throw subRefs.AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out bool _);
            return subRefs.QueryRefsByPathIntern<TValue>(path);
        }
        
        public SubRefsTask<TValue> QueryRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (synced)
                throw subRefs.AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out bool _);
            return subRefs.QueryRefsByPathIntern<TValue>(path);
        }
    }
}

