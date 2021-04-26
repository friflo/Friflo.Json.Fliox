// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;


namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryRefsTask -----------------------------------------
    public interface ISubRefsTask
    {
        string                              Selector    { get; }
        string                              Container   { get; }
        Dictionary<string, ISubRefsTask>    SubRefs     { get; }
        
        void    SetResult (EntitySet set, HashSet<string> ids);

    }

    public class SubRefsTask<T> : RefsTask<T>, ISubRefsTask where T : Entity
    {
        private   readonly  Dictionary<string, T>               results = new Dictionary<string, T>();
        private   readonly  ISetTask                            parent;
            
        public              Dictionary<string, T>               Results          => synced ? results      : throw RequiresSyncError("QueryRefsTask.Results requires Sync().");
        public              T                                   this[string id]  => synced ? results[id]  : throw RequiresSyncError("QueryRefsTask[] requires Sync().");
        
        public    override  string                              Label => $"{parent.Label} {Selector}";
        public    override  string                              ToString() => Label;
            
        public              string                              Selector  { get; }
        public              string                              Container { get; }
        public              Dictionary<string, ISubRefsTask>    SubRefs => subRefs;


        internal SubRefsTask(ISetTask parent, string selector, string container)
        {
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }

        public void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet<T>) set;
            synced = true;
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                results.Add(id, peer.entity);
            }
        }
    }
    
    public class SubRefTask<T> : RefsTask<T>, ISubRefsTask where T : Entity
    {
        private             string                              id;
        private             T                                   entity;
        private   readonly  ISetTask                            parent;

        public              string                              Id      => synced ? id      : throw RequiresSyncError("ReadRefTask.Id requires Sync().");
        public              T                                   Result  => synced ? entity  : throw RequiresSyncError("ReadRefTask.Result requires Sync().");
            
        public    override  string                              Label => $"{parent.Label} {Selector}";
        public    override  string                              ToString() => Label;
            
        public              string                              Selector  { get; }
        public              string                              Container { get; }
        public              Dictionary<string, ISubRefsTask>    SubRefs => subRefs;


        internal SubRefTask(ISetTask parent, string selector, string container)
        {
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }
        
        public void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet<T>) set;
            synced = true;
            if (ids.Count != 1)
                throw new InvalidOperationException($"Expect ids result with one element. Got: {ids.Count}");
            id = ids.First();
            var peer = entitySet.GetPeerById(id);
            entity = peer.entity;
        }
    }
    
}