// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.DB.Graph.Internal;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.Graph
{
    // ----------------------------------------- QueryTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class QueryTask<TKey, T> : SyncTask, IReadRefsTask<T> where T : class
    {
        internal            TaskState               state;
        internal            RefsTask                refsTask;
        internal readonly   FilterOperation         filter;
        internal readonly   string                  filterLinq; // use as string identifier of a filter 
        internal            Dictionary<TKey, T>     results;
        private  readonly   EntityStore             store;

        public              Dictionary<TKey, T>     Results         => IsOk("QueryTask.Result",  out Exception e) ? results      : throw e;
        public              T                       this[TKey key]  => IsOk("QueryTask[]",       out Exception e) ? results[key] : throw e;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"QueryTask<{typeof(T).Name}> (filter: {filterLinq})";
        

        internal QueryTask(FilterOperation filter, EntityStore store) {
            refsTask        = new RefsTask(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.store      = store;
        }

        public ReadRefsTask<TRefKey, TRef> ReadRefsPath<TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRefKey, TRef>(selector.path, store);
        }

        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey, TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
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

