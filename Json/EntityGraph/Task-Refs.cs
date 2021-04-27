// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    internal interface ISetTask
    {
        string  Label { get; }
    }
    
    public struct RefsTask
    {
        private readonly    ISetTask                            task;
        internal            bool                                synced;
        /// key: <see cref="IReadRefsTask.Selector"/>
        internal readonly   Dictionary<string, IReadRefsTask>   subRefs;
        
        
        internal RefsTask(ISetTask task) {
            this.task       = task;
            this.subRefs    = new Dictionary<string, IReadRefsTask>();
            this.synced     = false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already synced. {task.Label}");
        }
        
        internal Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {task.Label}");
        }
        
        public ReadRefsTask<TValue> ReadRefsByExpression<TValue>(Expression expression) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(expression, out _);
            return ReadRefsByPath<TValue>(path);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (subRefs.TryGetValue(selector, out IReadRefsTask subRefsTask))
                return (ReadRefsTask<TValue>)subRefsTask;
            var newQueryRefs = new ReadRefsTask<TValue>(task, selector, typeof(TValue).Name);
            subRefs.Add(selector, newQueryRefs);
            return newQueryRefs;
        }
    }
}