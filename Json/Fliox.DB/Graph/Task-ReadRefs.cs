// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.DB.Graph.Internal;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph
{

    // could be an interface, but than internal used methods would be public (C# 8.0 enables internal interface methods) 
    public abstract class ReadRefsTask : SyncTask
    {
        internal            TaskState   state;
        internal abstract   string      Selector    { get; }
        internal abstract   string      Container   { get; }
        internal abstract   SubRefs     SubRefs     { get; }
        
        internal abstract void    SetResult (EntitySet set, HashSet<JsonKey> ids);
    }

    /// ensure all tasks returning <see cref="ReadRefsTask{TKey,T}"/>'s provide the same interface
    public interface IReadRefsTask<T> where T : class
    {
        ReadRefsTask<TRefKey, TRef> ReadRefsPath   <TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector)                           where TRef : class;
        ReadRefsTask<TRefKey, TRef> ReadRefs       <TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector)              where TRef : class;
        ReadRefsTask<TRefKey, TRef> ReadArrayRefs  <TRefKey, TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class;
    }

    // ----------------------------------------- ReadRefsTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ReadRefsTask<TKey, T> : ReadRefsTask, IReadRefsTask<T>  where T : class
    {
        private             RefsTask                refsTask;
        private             Dictionary<TKey, T>     results;
        private   readonly  SyncTask                parent;
        private   readonly  EntityStore             store;
            
        public              Dictionary<TKey, T>     Results         => IsOk("ReadRefsTask.Results", out Exception e) ? results      : throw e;
        public              T                       this[TKey key]  => IsOk("ReadRefsTask[]",       out Exception e) ? results[key] : throw e;
        
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

        internal override void SetResult(EntitySet set, HashSet<JsonKey> ids) {
            var entitySet = (EntitySetBase<T>) set;
            results = new Dictionary<TKey, T>(ids.Count);
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                if (peer.error == null) {
                    var key = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                    results.Add(key, peer.Entity);
                } else {
                    entityErrorInfo.AddEntityError(peer.error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            }
        }
        
        // --- Refs
        public ReadRefsTask<TRefKey, TRef> ReadRefsPath<TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRefKey, TRef>(selector.path, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey,TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
        }
    }
    
    // ----------------------------------------- ReadRefTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ReadRefTask<TKey, T> : ReadRefsTask, IReadRefsTask<T> where T : class
    {
        private             RefsTask        refsTask;
        private             TKey            key;
        private             T               entity;
        private   readonly  SyncTask        parent;
        private   readonly  EntityStore     store;
    
        public              TKey            Key     => IsOk("ReadRefTask.Key",    out Exception e) ? key     : throw e;
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
        
        internal override void SetResult(EntitySet set, HashSet<JsonKey> ids) {
            var entitySet = (EntitySetBase<T>) set;
            if (ids.Count != 1)
                throw new InvalidOperationException($"Expect ids result set with one element. got: {ids.Count}, task: {this}");
            var id = ids.First();
            var peer = entitySet.GetPeerById(id);
            if (peer.error == null) {
                key     = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                entity  = peer.Entity;
            } else {
                var entityErrorInfo = new TaskErrorInfo();
                entityErrorInfo.AddEntityError(peer.error);
                state.SetError(entityErrorInfo);
            }
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadRefsPath<TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRefKey, TRef>(selector.path, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey, TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, store);
        }
    }
}
