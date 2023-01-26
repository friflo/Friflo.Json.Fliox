// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct Relations
    {
        private  readonly   SyncFunction    task;
        internal            SubRelations    subRelations;

        internal Relations(SyncFunction task) {
            this.task       = task ?? throw new InvalidOperationException("Expect task not null");
            this.subRelations    = new SubRelations();
        }

        internal ReadRelations<TRef> ReadRelationsByExpression<TRef>(EntitySet relation, Expression expression, FlioxClient store) where TRef : class {
            string path = ExpressionSelector.PathFromExpression(expression, out _);
            return ReadRelationsByPath<TRef>(relation, path, store);
        }
        
        internal ReadRelations<TRef> ReadRelationsByPath<TRef>(EntitySet relation, string selector, FlioxClient store) where TRef : class {
            if (subRelations.TryGetTask(selector, out ReadRelationsFunction readRelationsFunction))
                return (ReadRelations<TRef>)readRelationsFunction;
            // var relation = store._intern.GetSetByType(typeof(TValue));
            var keyName         = relation.GetKeyName();
            var isIntKey        = relation.IsIntKey();
            var readRelations   = new ReadRelations<TRef>(task, selector, relation.nameShort, keyName, isIntKey, store);
            subRelations.AddReadRelations(selector, readRelations);
            store.AddFunction(readRelations);
            return readRelations;
        }
    }
    
    internal struct SubRelations // : IEnumerable <BinaryPair>   <- not implemented to avoid boxing
    {
        /// key: <see cref="ReadRelationsFunction.Selector"/>
        private     Dictionary<string, ReadRelationsFunction>   map; // map == null if no tasks added
        
        internal    int                                         Count => map?.Count ?? 0;
        internal    ReadRelationsFunction                       this[string key] => map[key];
        
        internal bool TryGetTask(string selector, out ReadRelationsFunction subRelationsTask) {
            if (map == null) {
                subRelationsTask = null;
                return false;
            }
            return map.TryGetValue(selector, out subRelationsTask);
        }
        
        internal void AddReadRelations(string selector, ReadRelationsFunction readRelations) {
            if (map == null) {
                map = new Dictionary<string, ReadRelationsFunction>();
            }
            map.Add(selector, readRelations);
        }

        // return ValueIterator instead of IEnumerator<ReadRefsTask> to avoid boxing 
        public ValueIterator<string, ReadRelationsFunction> GetEnumerator() {
            return new ValueIterator<string, ReadRelationsFunction>(map);
        }
    }
}