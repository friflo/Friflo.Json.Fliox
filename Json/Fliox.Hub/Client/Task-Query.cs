// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Client
{
    // ----------------------------------------- QueryTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class QueryTask<TKey, T> : SyncTask, IReadRefsTask<T> where T : class
    {
        public              int?                    limit;
        /// <summary> return <see cref="maxCount"/> number of entities within <see cref="Result"/>.
        /// After task execution <see cref="ResultCursor"/> is not null if more entities available.
        /// To access them create new query and assign <see cref="ResultCursor"/> to its <see cref="cursor"/>.   
        /// </summary>
        public              int?                    maxCount;
        /// <summary> <see cref="cursor"/> is used to proceed iterating entities of a previous query
        /// which set <see cref="maxCount"/>. <br/>
        /// Therefore assign <see cref="ResultCursor"/> of the previous to <see cref="cursor"/>. </summary>
        public              string                  cursor;
        
        internal            TaskState               state;
        internal            RefsTask                refsTask;
        internal readonly   FilterOperation         filter;
        internal readonly   string                  filterLinq; // use as string identifier of a filter 
        internal            Dictionary<TKey, T>     result;
        internal            string                  resultCursor;
        private  readonly   FlioxClient             store;

        public              Dictionary<TKey, T>     Result          => IsOk("QueryTask.Result",  out Exception e) ? result      : throw e;
        public              T                       this[TKey key]  => IsOk("QueryTask[]",       out Exception e) ? result[key] : throw e;
        
        /// <summary> Is not null after task execution if more entities available.
        /// To access them create a new query and assign <see cref="ResultCursor"/> to its <see cref="cursor"/>. </summary>
        public              string                  ResultCursor    => IsOk("QueryTask.ResultCursor", out Exception e) ? resultCursor : throw e;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"QueryTask<{typeof(T).Name}> (filter: {filterLinq})";
        public              QueryFormat             DebugQuery      => filter.query;
        

        internal QueryTask(FilterOperation filter, FlioxClient store) {
            refsTask        = new RefsTask(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.store      = store;
        }

        public ReadRefsTask<TRefKey, TRef> ReadRefsPath<TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRefKey, TRef>(selector.path, store);
        }

        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey, TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
        }
    }
    
    
    public sealed class EntityFilter<T>
    {
        internal readonly FilterOperation op;

        public EntityFilter(Expression<Func<T, bool>> filter) {
            op = Operation.FromFilter(filter, EntitySet.RefQueryPath);
        }
    }    
    
}

