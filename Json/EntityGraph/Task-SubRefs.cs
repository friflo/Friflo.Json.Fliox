// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    public abstract class SubRefsBase<T> where T : Entity
    {
        internal            bool                                synced;
        internal readonly   EntitySet                           set;
        /// key: <see cref="ISubRefsTask.Selector"/>
        internal readonly   Dictionary<string, ISubRefsTask>    map;
        
        protected abstract  string                              Label { get; }

        internal SubRefsBase(EntitySet set) {

            this.set    = set;
            this.map    = new Dictionary<string, ISubRefsTask>();
        }
        
        private Exception AlreadySyncedError() {
            return new InvalidOperationException($"Used task is already synced. {Label}");
        }
        
        private SubRefsTask<TValue> SubRefsByExpression<TValue>(Expression selector) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(selector, out _);
            return SubRefsByPath<TValue>(path);
        }
        
        private SubRefsTask<TValue> SubRefsByPath<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            if (map.TryGetValue(selector, out ISubRefsTask subRefsTask))
                return (SubRefsTask<TValue>)subRefsTask;
            var newQueryRefs = new SubRefsTask<TValue>(Label, set, selector, typeof(TValue).Name);
            map.Add(selector, newQueryRefs);
            return newQueryRefs;
        }
        
        public SubRefsTask<TValue> SubRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            return SubRefsByExpression<TValue>(selector);
        }
        
        public SubRefsTask<TValue> SubRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            return SubRefsByExpression<TValue>(selector);
        }
    }

    // ----------------------------------------- QueryRefsTask -----------------------------------------
    internal interface ISubRefsTask
    {
        string Selector { get; }
        string Container { get; }
    }

    public class SubRefsTask<T> : ISubRefsTask where T : Entity
    {
        private  readonly   string      filterLinq; // todo rename to parentLabel
        private  readonly   EntitySet   parentSet;

        private             string      DebugName => $"{parentSet.name}['{filterLinq}'] {Selector}";
        public   override   string      ToString() => DebugName;
        
        internal            bool                    synced;
        internal readonly   Dictionary<string, T>   results = new Dictionary<string, T>();

        public              string                  Selector { get; }
        public              string                  Container { get; }

        public              Dictionary<string, T>   Results          => synced ? results      : throw RequiresSyncError("QueryRefsTask.Results requires Sync().");
        public              T                       this[string id]  => synced ? results[id]  : throw RequiresSyncError("QueryRefsTask[] requires Sync().");

        internal SubRefsTask(string filterLinq, EntitySet parentSet, string selector, string container)
        {
            this.filterLinq = filterLinq;
            this.parentSet  = parentSet;
            this.Selector   = selector;
            this.Container  = container;
        }
        
        protected Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {DebugName}");
        }


    }
    
}