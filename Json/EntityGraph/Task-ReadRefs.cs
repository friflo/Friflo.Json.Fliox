// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryRefsTask -----------------------------------------
    public interface IReadRefsTask
    {
        string                              Selector    { get; }
        string                              Container   { get; }
        Dictionary<string, IReadRefsTask>   SubRefs     { get; }
        
        void    SetResult (EntitySet set, HashSet<string> ids);

    }

    public class ReadRefsTask<T> : ISetTask, IReadRefsTask where T : Entity
    {
        private             RefsTask                            refsTask;
        private   readonly  Dictionary<string, T>               results = new Dictionary<string, T>();
        private   readonly  ISetTask                            parent;
            
        public              Dictionary<string, T>               Results          => refsTask.synced ? results      : throw refsTask.RequiresSyncError("ReadRefsTask.Results requires Sync().");
        public              T                                   this[string id]  => refsTask.synced ? results[id]  : throw refsTask.RequiresSyncError("ReadRefsTask[] requires Sync().");
        
        public              string                              Label => $"{parent.Label} > {Selector}";
        public    override  string                              ToString() => Label;
            
        public              string                              Selector  { get; }
        public              string                              Container { get; }
        public              Dictionary<string, IReadRefsTask>   SubRefs => refsTask.subRefs;


        internal ReadRefsTask(ISetTask parent, string selector, string container)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }

        public void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet<T>) set;
            refsTask.synced = true;
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                results.Add(id, peer.entity);
            }
        }
        
        // --- Refs
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
    
    public class ReadRefTask<T> : ISetTask, IReadRefsTask where T : Entity
    {
        private             RefsTask                            refsTask;
        private             string                              id;
        private             T                                   entity;
        private   readonly  ISetTask                            parent;

        public              string                              Id      => refsTask.synced ? id      : throw refsTask.RequiresSyncError("ReadRefTask.Id requires Sync().");
        public              T                                   Result  => refsTask.synced ? entity  : throw refsTask.RequiresSyncError("ReadRefTask.Result requires Sync().");
            
        public              string                              Label => $"{parent.Label} > {Selector}";
        public    override  string                              ToString() => Label;
            
        public              string                              Selector  { get; }
        public              string                              Container { get; }
        public              Dictionary<string, IReadRefsTask>   SubRefs => refsTask.subRefs;


        internal ReadRefTask(ISetTask parent, string selector, string container)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }
        
        public void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet<T>) set;
            refsTask.synced = true;
            if (ids.Count != 1)
                throw new InvalidOperationException($"Expect ids result set with one element. got: {ids.Count}, task: {this}");
            id = ids.First();
            var peer = entitySet.GetPeerById(id);
            entity = peer.entity;
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
    
}