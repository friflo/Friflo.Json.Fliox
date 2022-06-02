// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{

    // could be an interface, but than internal used methods would be public (C# 8.0 enables internal interface methods) 
    public abstract class ReadRefsTask : SyncTask
    {
        internal            TaskState   state;
        internal abstract   string      Selector    { get; }
        internal abstract   string      Container   { get; }
        internal abstract   string      KeyName     { get; }
        internal abstract   bool        IsIntKey    { get; }
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
    public sealed class ReadRefsTask<TKey, T> : ReadRefsTask, IReadRefsTask<T>  where T : class
    {
        private             RefsTask                refsTask;
        private             Dictionary<TKey, T>     result;
        private   readonly  SyncTask                parent;
        private   readonly  FlioxClient             store;
            
        public              Dictionary<TKey, T>     Result          => IsOk("ReadRefsTask.Result", out Exception e) ? result      : throw e;
        public              T                       this[TKey key]  => IsOk("ReadRefsTask[]",      out Exception e) ? result[key] : throw e;
        
        internal  override  TaskState               State       => state;
        public    override  string                  Details     => $"{parent.GetLabel()} -> {Selector}";
            
        internal  override  string                  Selector  { get; }
        internal  override  string                  Container { get; }
        internal  override  string                  KeyName   { get; }
        internal  override  bool                    IsIntKey  { get; }
        
        internal  override  SubRefs                 SubRefs => refsTask.subRefs;

        private static readonly     KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();

        internal ReadRefsTask(SyncTask parent, string selector, string container, string keyName, bool isIntKey, FlioxClient store)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
            this.KeyName    = keyName;
            this.IsIntKey   = isIntKey;
            this.store      = store;
        }

        internal override void SetResult(EntitySet set, HashSet<JsonKey> ids) {
            var entitySet = (EntitySetBase<T>) set;
            result = SyncSet.CreateDictionary<TKey, T>(ids.Count);
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                if (peer.error == null) {
                    var key = KeyConvert.IdToKey(id);
                    result.Add(key, peer.Entity);
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
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByPath<TRefKey, TRef>(relation, selector.path, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(relation, selector, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey,TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(relation, selector, store);
        }
    }
    
    // ----------------------------------------- ReadRefTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ReadRefTask<TKey, T> : ReadRefsTask, IReadRefsTask<T> where T : class
    {
        private             RefsTask        refsTask;
        private             TKey            key;
        private             T               entity;
        private   readonly  SyncTask        parent;
        private   readonly  FlioxClient     store;
    
        public              TKey            Key     => IsOk("ReadRefTask.Key",    out Exception e) ? key     : throw e;
        public              T               Result  => IsOk("ReadRefTask.Result", out Exception e) ? entity  : throw e;
                
        internal  override  TaskState       State       => state;
        public    override  string          Details     => $"{parent.GetLabel()} -> {Selector}";
                
        internal  override  string          Selector    { get; }
        internal  override  string          Container   { get; }
        internal  override  string          KeyName     { get; }
        internal  override  bool            IsIntKey    { get; }

        internal override   SubRefs         SubRefs => refsTask.subRefs;

        private static readonly     KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();

        internal ReadRefTask(SyncTask parent, string selector, string container, string keyName, bool isIntKey, FlioxClient store)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
            this.KeyName    = keyName;
            this.IsIntKey   = isIntKey;
            this.store      = store;
        }
        
        internal override void SetResult(EntitySet set, HashSet<JsonKey> ids) {
            if (ids.Count == 0)
                return;
            if (ids.Count != 1)
                throw new InvalidOperationException($"Expect ids result set with one element. got: {ids.Count}, task: {this}");
            var entitySet = (EntitySetBase<T>) set;
            var id = ids.First();
            var peer = entitySet.GetPeerById(id);
            if (peer.error == null) {
                key     = KeyConvert.IdToKey(id);
                entity  = peer.Entity;
            } else {
                var entityErrorInfo = new TaskErrorInfo();
                entityErrorInfo.AddEntityError(peer.error);
                state.SetError(entityErrorInfo);
            }
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadRefsPath<TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByPath<TRefKey, TRef>(relation, selector.path, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(relation, selector, store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey, TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(relation, selector, store);
        }
    }
}
