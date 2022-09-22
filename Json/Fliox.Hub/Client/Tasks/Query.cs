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
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class QueryTask<T> : SyncTask, IReadRelationsTask<T> where T : class
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
        internal readonly   FilterOperation filter;
        internal readonly   string          filterLinq; // use as string identifier of a filter 
        internal            List<T>         result;
        internal            IReadOnlyCollection<JsonKey>        ids;
        internal            Dictionary<JsonKey, EntityValue>    entities;
        internal            string          resultCursor;
        private  readonly   FlioxClient     store;
        private  readonly   SyncSetBase<T>  syncSet;

        public              List<T>         Result          => IsOk("QueryTask.Result",   out Exception e) ? result : throw e;
        public              List<JsonValue> RawResult       => IsOk("QueryTask.RawResult",out Exception e) ? GetRawValues() : throw e;
        
        /// <summary> Is not null after task execution if more entities available.
        /// To access them create a new query and assign <see cref="ResultCursor"/> to its <see cref="cursor"/>. </summary>
        public              string          ResultCursor    => IsOk("QueryTask.ResultCursor", out Exception e) ? resultCursor : throw e;
            
        internal override   TaskState       State           => state;
        public   override   string          Details         => $"QueryTask<{typeof(T).Name}> (filter: {filterLinq})";
        internal override   TaskType        TaskType        => TaskType.query;
        public              QueryFormat     DebugQuery      => filter.query;
        

        internal QueryTask(FilterOperation filter, FlioxClient store, SyncSetBase<T> syncSet) {
            relations       = new Relations(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.store      = store;
            this.syncSet    = syncSet;
        }

        private List<JsonValue> GetRawValues() {
            var jsonResult = new List<JsonValue> (ids.Count);
            foreach (var id in ids) {
                var entity = entities[id];
                jsonResult.Add(entity.Json);
            }
            return jsonResult;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return syncSet.QueryEntities(this, context);
        }
        
        // --- IReadRelationsTask<T>
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class  where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByPath<TRef>(relation, selector.path, store);
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

