// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    internal readonly struct SubRefs
    {
        internal readonly   EntitySet                           set;
        private  readonly   string                              label;
        /// key: <see cref="SubRefsTask.selector"/>
        internal readonly   Dictionary<string, SubRefsTask>     map;

        internal SubRefs(string label, EntitySet set) {
            this.label  = label;
            this.set    = set;
            this.map    = new Dictionary<string, SubRefsTask>();
        }
        
        internal Exception AlreadySyncedError() {
            return new InvalidOperationException($"Used QueryTask is already synced. QueryTask<{set.name}>, filter: {label}");
        }
        
        internal SubRefsTask<TValue> SubRefsByExpression<TValue>(Expression selector) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(selector, out _);
            return SubRefsByPath<TValue>(path);
        }
        
        internal SubRefsTask<TValue> SubRefsByPath<TValue>(string selector) where TValue : Entity {
            if (map.TryGetValue(selector, out SubRefsTask subRefsTask))
                return (SubRefsTask<TValue>)subRefsTask;
            var newQueryRefs = new SubRefsTask<TValue>(label, set, selector, typeof(TValue));
            map.Add(selector, newQueryRefs);
            return newQueryRefs;
        }
    }

    interface ISubRefsTask<T> where T : Entity
    {
        SubRefsTask<TValue> SubRefsByPath <TValue>(string selector)                                        where TValue : Entity;
        SubRefsTask<TValue> SubRef        <TValue>(Expression<Func<T, Ref<TValue>>> selector)              where TValue : Entity;
        SubRefsTask<TValue> SubRefs       <TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity;
    }
    
    // ----------------------------------------- QueryRefsTask -----------------------------------------
    public class SubRefsTask
    {
        internal readonly   string      selector;
        internal readonly   Type        entityType;
        private  readonly   string      filterLinq; // todo rename to parentLabel
        private  readonly   EntitySet   parentSet;

        private             string      DebugName => $"{parentSet.name}['{filterLinq}'] {selector}";
        public   override   string      ToString() => DebugName;
        
        internal SubRefsTask(string filterLinq, EntitySet parentSet, string selector, Type entityType) {
            this.filterLinq         = filterLinq;
            this.parentSet          = parentSet;
            this.selector           = selector;
            this.entityType         = entityType;
        }
        
        protected Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {DebugName}");
        }
    }

    public class SubRefsTask<T> : SubRefsTask where T : Entity
    {
        internal            bool                    synced;
        internal readonly   Dictionary<string, T>   results = new Dictionary<string, T>();
        
        public              Dictionary<string, T>   Results          => synced ? results      : throw RequiresSyncError("QueryRefsTask.Results requires Sync().");
        public              T                       this[string id]  => synced ? results[id]  : throw RequiresSyncError("QueryRefsTask[] requires Sync().");

        internal SubRefsTask(string filterLinq, EntitySet parentSet, string selector, Type entityType) :
            base (filterLinq, parentSet, selector, entityType) { }
    }
    
}