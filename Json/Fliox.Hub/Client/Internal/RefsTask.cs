// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct RefsTask
    {
        private  readonly   SyncFunction    task;
        internal            SubRefs         subRefs;

        internal RefsTask(SyncFunction task) {
            this.task       = task ?? throw new InvalidOperationException("Expect task not null");
            this.subRefs    = new SubRefs();
        }

        internal ReadRelations<TRef> ReadRefsByExpression<TRef>(EntitySet relation, Expression expression, FlioxClient store) where TRef : class {
            string path = ExpressionSelector.PathFromExpression(expression, out _);
            return ReadRefsByPath<TRef>(relation, path, store);
        }
        
        internal ReadRelations<TRef> ReadRefsByPath<TRef>(EntitySet relation, string selector, FlioxClient store) where TRef : class {
            if (subRefs.TryGetTask(selector, out ReadRelationsFunction subRelationsTask))
                return (ReadRelations<TRef>)subRelationsTask;
            // var relation = store._intern.GetSetByType(typeof(TValue));
            var keyName     = relation.GetKeyName();
            var isIntKey    = relation.IsIntKey();
            var newQueryRefs = new ReadRelations<TRef>(task, selector, relation.name, keyName, isIntKey, store);
            subRefs.AddTask(selector, newQueryRefs);
            store.AddFunction(newQueryRefs);
            return newQueryRefs;
        }
    }
    
    internal struct SubRefs // : IEnumerable <BinaryPair>   <- not implemented to avoid boxing
    {
        /// key: <see cref="ReadRelationsFunction.Selector"/>
        private     Dictionary<string, ReadRelationsFunction>   map; // map == null if no tasks added
        
        internal    int                                     Count => map?.Count ?? 0;
        internal    ReadRelationsFunction                       this[string key] => map[key];
        
        internal bool TryGetTask(string selector, out ReadRelationsFunction subRelationsTask) {
            if (map == null) {
                subRelationsTask = null;
                return false;
            }
            return map.TryGetValue(selector, out subRelationsTask);
        }
        
        internal void AddTask(string selector, ReadRelationsFunction subRelationsTask) {
            if (map == null) {
                map = new Dictionary<string, ReadRelationsFunction>();
            }
            map.Add(selector, subRelationsTask);
        }

        // return ValueIterator instead of IEnumerator<ReadRefsTask> to avoid boxing 
        public ValueIterator<string, ReadRelationsFunction> GetEnumerator() {
            return new ValueIterator<string, ReadRelationsFunction>(map);
        }
    }
}