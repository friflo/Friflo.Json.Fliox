// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- QueryTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class QueryTask<T> : SyncTask, IReadRefsTask<T> where T : Entity
    {
        internal            bool                    synced;
        internal            RefsTask                refsTask;
        internal readonly   FilterOperation         filter;
        internal readonly   string                  filterLinq; // use as string identifier of a filter 
        internal            Dictionary<string, T>   entities;

        public              Dictionary<string, T>   Results         => Synced ? entities     : throw RequiresSyncError("QueryTask.Result requires Sync().");
        public              T                       this[string id] => Synced ? entities[id] : throw RequiresSyncError("QueryTask[] requires Sync().");
            
        internal override   bool                    Synced          => synced;
        internal override   string                  Label           => $"QueryTask<{typeof(T).Name}> filter: {filterLinq}";
        public   override   string                  ToString()      => Label;


        internal QueryTask(FilterOperation filter) {
            refsTask        = new RefsTask(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }

        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadArrayRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
}

