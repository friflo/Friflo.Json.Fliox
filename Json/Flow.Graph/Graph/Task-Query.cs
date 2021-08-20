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
    public class QueryTask<T> : SyncTask, IReadRefsTask<T> where T : class
    {
        internal            TaskState               state;
        internal            RefsTask                refsTask;
        internal readonly   FilterOperation         filter;
        internal readonly   string                  filterLinq; // use as string identifier of a filter 
        internal            Dictionary<string, T>   results;
        private  readonly   EntityStore             store;

        public              Dictionary<string, T>   Results         => IsOk("QueryTask.Result",  out Exception e) ? results     : throw e;
        public              T                       this[string id] => IsOk("QueryTask[]",       out Exception e) ? results[id] : throw e;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"QueryTask<{typeof(T).Name}> (filter: {filterLinq})";
        

        internal QueryTask(FilterOperation filter, EntityStore store) {
            refsTask        = new RefsTask(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.store      = store;
        }

        public ReadRefsTask<TKey2, TRef> ReadRefsPath<TKey2, TRef>(RefsPath<TKey2, T, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TKey2, TRef>(selector.path, store);
        }

        public ReadRefsTask<TKey2, TRef> ReadRefs<TKey2, TRef>(Expression<Func<T, Ref<TKey2, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TKey2, TRef>(selector, store);
        }
        
        public ReadRefsTask<TKey2, TRef> ReadArrayRefs<TKey2, TRef>(Expression<Func<T, IEnumerable<Ref<TKey2, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TKey2, TRef>(selector, store);
        }
    }
    
    
    public class EntityFilter<T>
    {
        internal readonly FilterOperation op;

        public EntityFilter(Expression<Func<T, bool>> filter) {
            op = Operation.FromFilter(filter, EntitySet.RefQueryPath);
        }
    }    
    
}

