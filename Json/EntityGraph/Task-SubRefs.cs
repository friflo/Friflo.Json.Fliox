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
    
    public abstract class RefsBase<T> : ISetTask where T : Entity
    {
        internal            bool                                synced;
        /// key: <see cref="ISubRefsTask.Selector"/>
        internal readonly   Dictionary<string, ISubRefsTask>    subRefs;
        
        public   abstract   string                              Label { get; }

        internal RefsBase() {
            this.subRefs    = new Dictionary<string, ISubRefsTask>();
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
            if (subRefs.TryGetValue(selector, out ISubRefsTask subRefsTask))
                return (SubRefsTask<TValue>)subRefsTask;
            var newQueryRefs = new SubRefsTask<TValue>(this, selector, typeof(TValue).Name);
            subRefs.Add(selector, newQueryRefs);
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
    public interface ISubRefsTask
    {
        string                              Selector    { get; }
        string                              Container   { get; }
        Dictionary<string, ISubRefsTask>    SubRefs     { get; }
    }

    public class SubRefsTask<T> : RefsBase<T>, ISubRefsTask where T : Entity
    {
        private   readonly  ISetTask                            parent;
        internal  readonly  Dictionary<string, T>               results = new Dictionary<string, T>();
            
        public    override  string                              Label => $"{parent.Label} {Selector}";
        public    override  string                              ToString() => Label;
            
        public              string                              Selector  { get; }
        public              string                              Container { get; }
        public              Dictionary<string, ISubRefsTask>    SubRefs => subRefs;

        public              Dictionary<string, T>   Results          => synced ? results      : throw RequiresSyncError("QueryRefsTask.Results requires Sync().");
        public              T                       this[string id]  => synced ? results[id]  : throw RequiresSyncError("QueryRefsTask[] requires Sync().");

        internal SubRefsTask(ISetTask parent, string selector, string container)
        {
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }
        
        protected Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {Label}");
        }


        
    }
    
}