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
        internal readonly   FilterOperation                     filter;
        internal readonly   string                              filterLinq; // use as string identifier of a filter 
        private  readonly   EntitySet<T>                        set;
        internal            bool                                synced;
        internal readonly   List<T>                             entities = new List<T>();
        /// key: <see cref="QueryRefsTask.selector"/>
        internal readonly   Dictionary<string, QueryRefsTask>   queryRefs = new Dictionary<string, QueryRefsTask>();
        
        public              List<T>                             Result          => synced ? entities        : throw RequiresSyncError("QueryTask.Result requires Sync().");
        public              T                                   this[int index] => synced ? entities[index] : throw RequiresSyncError("QueryTask[] requires Sync().");
                     
        public override     string                              ToString() => filterLinq;

        internal QueryTask(FilterOperation filter, EntitySet<T> set) {
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.set        = set;
        }
        
        private Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} Entity: {set.name} filter: {filterLinq}");
        }
        
        private Exception AlreadySyncedError() {
            return new InvalidOperationException($"Used QueryTask is already synced. QueryTask<{typeof(T).Name}>, filter: {filterLinq}");
        }
        
        // --- schedule query references
        public QueryRefsTask<TValue> QueryRefsByPath<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            return QueryRefsByPathIntern<TValue>(selector);
        }
        
        public QueryRefsTask<TValue> QueryRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            // if (!isArraySelector)
            //     throw new InvalidOperationException($"selector returns a single ReadRef. Use ${nameof(ReadRef)}()");
            return QueryRefsByPathIntern<TValue>(path);
        }
        
        public QueryRefsTask<TValue> QueryRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            // if (!isArraySelector)
            //     throw new InvalidOperationException($"selector returns a single ReadRef. Use ${nameof(ReadRef)}()");
            return QueryRefsByPathIntern<TValue>(path);
        }

        private QueryRefsTask<TValue> QueryRefsByPathIntern<TValue>(string selector) where TValue : Entity {
            if (queryRefs.TryGetValue(selector, out QueryRefsTask readRef))
                return (QueryRefsTask<TValue>)readRef;
            var newQueryRefs = new QueryRefsTask<TValue>(filterLinq, set, selector, typeof(TValue));
            queryRefs.Add(selector, newQueryRefs);
            return newQueryRefs;
        }
    }
}

