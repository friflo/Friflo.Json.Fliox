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
        internal            TaskState   state;
        internal abstract   string      Selector    { get; }
        internal abstract   string      Container   { get; }
        internal abstract   SubRefs     SubRefs     { get; }
        
        internal abstract void    SetResult (EntitySet set, HashSet<string> ids);
    }

    /// ensure all tasks returning <see cref="ReadRefsTask{T}"/>'s provide the same interface
    public interface IReadRefsTask<T> where T : class
    {
        ReadRefsTask<TRef> ReadRefsPath   <TKey2, TRef>(RefsPath<TKey2, T, TRef> selector)                           where TRef : class;
        ReadRefsTask<TRef> ReadRefs       <TKey2, TRef>(Expression<Func<T, Ref<TKey2, TRef>>> selector)              where TRef : class;
        ReadRefsTask<TRef> ReadArrayRefs  <TKey2, TRef>(Expression<Func<T, IEnumerable<Ref<TKey2, TRef>>>> selector) where TRef : class;
    }

    // ----------------------------------------- ReadRefsTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ReadRefsTask<T> : ReadRefsTask, IReadRefsTask<T>  where T : class
    {
        private             RefsTask                refsTask;
        private             Dictionary<string, T>   results;
        private   readonly  SyncTask                parent;
        private   readonly  EntityStore             store;
            
        public              Dictionary<string, T>   Results          => IsOk("ReadRefsTask.Results", out Exception e) ? results     : throw e;
        public              T                       this[string id]  => IsOk("ReadRefsTask[]",       out Exception e) ? results[id] : throw e;
        
        internal  override  TaskState               State       => state;
        public    override  string                  Details     => $"{parent.GetLabel()} -> {Selector}";
            
        internal  override  string                  Selector  { get; }
        internal  override  string                  Container { get; }
        
        internal  override  SubRefs                 SubRefs => refsTask.subRefs;


        internal ReadRefsTask(SyncTask parent, string selector, string container, EntityStore store)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
            this.store      = store;
        }

        internal override void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet2<T>) set;
            results = new Dictionary<string, T>(ids.Count);
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                if (peer.error == null) {
                    results.Add(id, peer.Entity);
                } else {
                    entityErrorInfo.AddEntityError(peer.error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            }
        }
        
        // --- Refs
        public ReadRefsTask<TRef> ReadRefsPath<TKey2, TRef>(RefsPath<TKey2, T, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(selector.path, store);
        }
        
        public ReadRefsTask<TRef> ReadRefs<TKey2, TRef>(Expression<Func<T, Ref<TKey2, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector, store);
        }
        
        public ReadRefsTask<TRef> ReadArrayRefs<TKey2,TRef>(Expression<Func<T, IEnumerable<Ref<TKey2, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector, store);
        }
    }
    
    // ----------------------------------------- ReadRefTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ReadRefTask<TKey, T> : ReadRefsTask, IReadRefsTask<T> where T : class
    {
        private             RefsTask        refsTask;
        private             string          id;
        private             T               entity;
        private   readonly  SyncTask        parent;
        private   readonly  EntityStore     store;
    
        public              string          Id      => IsOk("ReadRefTask.Id",     out Exception e) ? id      : throw e;
        public              T               Result  => IsOk("ReadRefTask.Result", out Exception e) ? entity  : throw e;
                
        internal override   TaskState       State       => state;
        public   override   string          Details     => $"{parent.GetLabel()} -> {Selector}";
                
        internal override   string          Selector  { get; }
        internal override   string          Container { get; }
            
        internal override   SubRefs         SubRefs => refsTask.subRefs;


        internal ReadRefTask(SyncTask parent, string selector, string container, EntityStore store)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
            this.store      = store;
        }
        
        internal override void SetResult(EntitySet set, HashSet<string> ids) {
            var entitySet = (EntitySet<TKey, T>) set;
            if (ids.Count != 1)
                throw new InvalidOperationException($"Expect ids result set with one element. got: {ids.Count}, task: {this}");
            id = ids.First();
            var peer = entitySet.GetPeerById(id);
            if (peer.error == null) {
                entity = peer.Entity;
            } else {
                var entityErrorInfo = new TaskErrorInfo();
                entityErrorInfo.AddEntityError(peer.error);
                state.SetError(entityErrorInfo);
            }
        }
        
        public ReadRefsTask<TRef> ReadRefsPath<TKey2, TRef>(RefsPath<TKey2, T, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(selector.path, store);
        }
        
        public ReadRefsTask<TRef> ReadRefs<TKey2, TRef>(Expression<Func<T, Ref<TKey2, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector, store);
        }
        
        public ReadRefsTask<TRef> ReadArrayRefs<TKey2, TRef>(Expression<Func<T, IEnumerable<Ref<TKey2, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector, store);
        }
    }
}
