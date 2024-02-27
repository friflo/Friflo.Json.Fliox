// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // ----------------------------------------- QueryTask -----------------------------------------
    [CLSCompliant(true)]
    public sealed class QueryTask<TKey, T> : SyncTask, IRelationsParent, IReadRelationsTask<T> where T : class
    {
        public              int?            limit;
        /// <summary> return <see cref="maxCount"/> number of entities within <see cref="Result"/>.
        /// After task execution <see cref="ResultCursor"/> is not null if more entities available.
        /// To access them create new query and assign <see cref="ResultCursor"/> to its <see cref="cursor"/>.   
        /// </summary>
        public              int?            maxCount;
        /// <summary> <see cref="cursor"/> is used to proceed iterating entities of a previous query
        /// which set <see cref="maxCount"/>. <br/>
        /// Therefore assign <see cref="ResultCursor"/> of the previous to <see cref="cursor"/>. </summary>
        public              string          cursor;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal            Relations       relations;
        public   readonly   FilterOperation filter;
        public   readonly   string          filterLinq; // use as string identifier of a filter 
        internal            List<T>         result;
        internal            string          sql;
        internal            EntityValue[]   entities;
        internal            string          resultCursor;
        private  readonly   FlioxClient     client;
        private  readonly   Set<TKey,T>     set;

        public              List<T>         Result          => IsOk("QueryTask.Result",   out Exception e) ? result   : throw e;
        public              EntityValue[]   RawResult       => IsOk("QueryTask.RawResult",out Exception e) ? entities : throw e;
        public              bool            IsFinished      => GetIsFinished();
        public              string          SQL             => IsOk("QueryTask.SQL",      out Exception e) ? sql : throw e;
        
        /// <summary> Is not null after task execution if more entities available.
        /// To access them create a new query and assign <see cref="ResultCursor"/> to its <see cref="cursor"/>. </summary>
        public              string          ResultCursor    => IsOk("QueryTask.ResultCursor", out Exception e) ? resultCursor : throw e;
            
        internal override   TaskState       State           => state;
        public              string          Label           => taskName ?? Details;
        public   override   string          Details         => $"QueryTask<{typeof(T).Name}> (filter: {filterLinq})";
        internal override   TaskType        TaskType        => TaskType.query;
        public              SyncTask        Task            => this;
      

        internal QueryTask(FilterOperation filter, FlioxClient client, Set<TKey,T> set) : base(set) {
            relations       = new Relations(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.client     = client;
            this.set  = set;
        }
        
        private QueryTask(QueryTask<TKey, T> query, Set<TKey,T> set) : base(set) {
            relations       = new Relations(this);
            filter          = query.filter;
            filterLinq      = query.filterLinq;
            client          = query.client;
            this.set        = set;
        }
        
        private bool GetIsFinished() {
            if (State.IsExecuted()) {
                if (!State.Error.HasErrors) {
                    return resultCursor == null;
                }
                throw new TaskResultException(State.Error.TaskError);
            }
            return false;
        }

        public QueryTask<TKey, T> QueryNext() {
            if (IsOk("QueryTask.QueryNext()", out Exception e)) {
                if (resultCursor == null) throw new InvalidOperationException("cursor query reached end");
                var query       = new QueryTask<TKey, T>(this, set) { cursor = resultCursor, maxCount = maxCount };
                client.AddTask(query);
                return query;
            }
            throw e;
        }

        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return set.QueryEntities(this, context);
        }
        
        // --- IReadRelationsTask<T>
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class  where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByPath<TRef>(relation.GetInstance(), selector.path, client, this);
        }
    }
    
    
    public sealed class EntityFilter<T>
    {
        internal readonly   FilterOperation op;

        public   override   string          ToString() => op.ToString();

        public EntityFilter(Expression<Func<T, bool>> filter) {
            op = Operation.FromFilter(filter, ClientStatic.RefQueryPath);
        }
    }    
    
}

