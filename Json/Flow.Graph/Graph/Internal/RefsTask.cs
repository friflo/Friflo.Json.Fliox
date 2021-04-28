// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal interface ISetTask
    {
        string  Label { get; }
    }
    
    internal struct RefsTask
    {
        private readonly    ISetTask                            task;
        internal            bool                                synced;
        /// key: <see cref="ReadRefsTask.Selector"/>
        internal            SubRefs                             subRefs;
        
        
        internal RefsTask(ISetTask task) {
            this.task       = task;
            this.subRefs    = new SubRefs();
            this.synced     = false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already synced. {task.Label}");
        }
        
        internal Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {task.Label}");
        }
        
        internal ReadRefsTask<TValue> ReadRefsByExpression<TValue>(Expression expression) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(expression, out _);
            return ReadRefsByPath<TValue>(path);
        }
        
        internal ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (subRefs.TryGetTask(selector, out ReadRefsTask subRefsTask))
                return (ReadRefsTask<TValue>)subRefsTask;
            var newQueryRefs = new ReadRefsTask<TValue>(task, selector, typeof(TValue).Name);
            subRefs.AddTask(selector, newQueryRefs);
            return newQueryRefs;
        }
    }
}