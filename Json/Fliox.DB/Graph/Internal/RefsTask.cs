// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    internal struct RefsTask
    {
        private  readonly   SyncTask    task;
        internal            SubRefs     subRefs;

        internal RefsTask(SyncTask task) {
            this.task       = task ?? throw new InvalidOperationException("Expect task not null");
            this.subRefs    = new SubRefs();
        }

        internal ReadRefsTask<TKey, TValue> ReadRefsByExpression<TKey, TValue>(Expression expression, EntityStore store) where TValue : class {
            string path = ExpressionSelector.PathFromExpression(expression, out _);
            return ReadRefsByPath<TKey, TValue>(path, store);
        }
        
        internal ReadRefsTask<TKey, TValue> ReadRefsByPath<TKey, TValue>(string selector, EntityStore store) where TValue : class {
            if (subRefs.TryGetTask(selector, out ReadRefsTask subRefsTask))
                return (ReadRefsTask<TKey, TValue>)subRefsTask;
            var set         = store._intern.setByType[typeof(TValue)];
            var keyName     = set.GetKeyName();
            var isIntKey    = set.IsIntKey();
            var newQueryRefs = new ReadRefsTask<TKey, TValue>(task, selector, set.name, keyName, isIntKey, store);
            subRefs.AddTask(selector, newQueryRefs);
            store.AddTask(newQueryRefs);
            return newQueryRefs;
        }
    }
    
    public struct SubRefs // : IEnumerable <BinaryPair>   <- not implemented to avoid boxing
    {
        /// key: <see cref="ReadRefsTask.Selector"/>
        private     Dictionary<string, ReadRefsTask>  map; // map == null if no tasks added
        
        public    int                                 Count => map?.Count ?? 0;
        public    ReadRefsTask                        this[string key] => map[key];
        
        public bool TryGetTask(string selector, out ReadRefsTask subRefsTask) {
            if (map == null) {
                subRefsTask = null;
                return false;
            }
            return map.TryGetValue(selector, out subRefsTask);
        }
        
        public void AddTask(string selector, ReadRefsTask subRefsTask) {
            if (map == null) {
                map = new Dictionary<string, ReadRefsTask>();
            }
            map.Add(selector, subRefsTask);
        }

        // return ValueIterator instead of IEnumerator<ReadRefsTask> to avoid boxing 
        public ValueIterator<string, ReadRefsTask> GetEnumerator() {
            return new ValueIterator<string, ReadRefsTask>(map);
        }
    }
}