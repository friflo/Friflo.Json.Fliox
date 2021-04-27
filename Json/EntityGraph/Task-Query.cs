// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryTask -----------------------------------------
    public class QueryTask<T> : ISetTask, IReadRefsTask<T> where T : Entity
    {
        internal            RefsTask            refsTask;
        internal readonly   FilterOperation     filter;
        internal readonly   string              filterLinq; // use as string identifier of a filter 
        internal readonly   List<T>             entities = new List<T>();

        public              List<T>             Result          => refsTask.synced ? entities        : throw refsTask.RequiresSyncError("QueryTask.Result requires Sync().");
        public              T                   this[int index] => refsTask.synced ? entities[index] : throw refsTask.RequiresSyncError("QueryTask[] requires Sync().");
        
        public              string              Label           => $"QueryTask<{typeof(T).Name}> filter: {filterLinq}";
        public   override   string              ToString()      => Label;


        internal QueryTask(FilterOperation filter) {
            refsTask        = new RefsTask(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
}

