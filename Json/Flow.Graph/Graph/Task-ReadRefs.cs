// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{

    
    // could be an interface, but than internal used methods would be public (C# 8.0 enables internal interface methods) 
    public abstract class ReadRefsTask : SyncTask
    {
        internal abstract string        Selector    { get; }
        internal abstract string        Container   { get; }
        internal abstract SubRefs       SubRefs     { get; }
        
        internal abstract void    SetResult (EntitySet set, HashSet<string> ids);
    }

    /// ensure all tasks returning <see cref="ReadRefsTask{T}"/>'s provide the same interface
    public interface IReadRefsTask<T> where T : Entity
    {
        ReadRefsTask<TValue> ReadRefs       <TValue>(Expression<Func<T, Ref<TValue>>> selector)              where TValue : Entity;
        ReadRefsTask<TValue> ReadArrayRefs  <TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity;
        ReadRefsTask<TValue> ReadRefsByPath <TValue>(string selector)                                        where TValue : Entity;
    }

    // ----------------------------------------- ReadRefsTask<T> -----------------------------------------
    public class ReadRefsTask<T> : ReadRefsTask, IReadRefsTask<T>  where T : Entity
    {
        private             RefsTask                refsTask;
        private             Dictionary<string, T>   results;
        private   readonly  SyncTask                parent;
            
        public              Dictionary<string, T>   Results          => Synced ? results      : throw RequiresSyncError("ReadRefsTask.Results requires Sync().");
        public              T                       this[string id]  => Synced ? results[id]  : throw RequiresSyncError("ReadRefsTask[] requires Sync().");
        
        internal  override  bool                    Synced      => parent.Synced;
        internal  override  string                  Label       => $"{parent.Label} > {Selector}";
        public    override  string                  ToString()  => Label;
            
        internal  override  string                  Selector  { get; }
        internal  override  string                  Container { get; }
        
        internal  override  SubRefs                 SubRefs => refsTask.subRefs;


        internal ReadRefsTask(SyncTask parent, string selector, string container)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }

        internal override void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet<T>) set;
            results = new Dictionary<string, T>(ids.Count);
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                results.Add(id, peer.entity);
            }
        }
        
        // --- Refs
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadArrayRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
    
    // ----------------------------------------- ReadRefTask<T> -----------------------------------------
    public class ReadRefTask<T> : ReadRefsTask, IReadRefsTask<T> where T : Entity
    {
        private             RefsTask    refsTask;
        private             string      id;
        private             T           entity;
        private   readonly  SyncTask    parent;

        public              string      Id      => Synced ? id      : throw RequiresSyncError("ReadRefTask.Id requires Sync().");
        public              T           Result  => Synced ? entity  : throw RequiresSyncError("ReadRefTask.Result requires Sync().");
                
        internal override   bool        Synced      => parent.Synced;
        internal override   string      Label       => $"{parent.Label} > {Selector}";
        public   override   string      ToString()  => Label;
                
        internal override   string      Selector  { get; }
        internal override   string      Container { get; }
            
        internal override   SubRefs     SubRefs => refsTask.subRefs;


        internal ReadRefTask(SyncTask parent, string selector, string container)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }
        
        internal override void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet<T>) set;
            if (ids.Count != 1)
                throw new InvalidOperationException($"Expect ids result set with one element. got: {ids.Count}, task: {this}");
            id = ids.First();
            var peer = entitySet.GetPeerById(id);
            entity = peer.entity;
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadArrayRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
    
}