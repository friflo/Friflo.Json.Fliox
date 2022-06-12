// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // ----------------------------------------- ReadRefTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ReadRelationTask<T> : ReadRelationsTask, IReadRelationsTask<T> where T : class
    {
        private             RefsTask    refsTask;
        private             T           entity;
        private   readonly  SyncTask    parent;
        private   readonly  FlioxClient store;
    
        public              T           Result  => IsOk("ReadRelationTask.Result", out Exception e) ? entity  : throw e;
                
        internal  override  TaskState   State   => state;
        public    override  string      Details => $"{parent.GetLabel()} -> {Selector}";
                
        internal  override  string      Selector    { get; }
        internal  override  string      Container   { get; }
        internal  override  string      KeyName     { get; }
        internal  override  bool        IsIntKey    { get; }

        internal override   SubRefs     SubRefs => refsTask.subRefs;

        internal ReadRelationTask(SyncTask parent, string selector, string container, string keyName, bool isIntKey, FlioxClient store)
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
            var id      = ids.First();
            var peer    = entitySet.GetPeerById(id);
            if (peer.error == null) {
                entity  = peer.Entity;
            } else {
                var entityErrorInfo = new TaskErrorInfo();
                entityErrorInfo.AddEntityError(peer.error);
                state.SetError(entityErrorInfo);
            }
        }
        
        // --- IReadRelationsTask<T>
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(relation, selector.path, store);
        }
    }
}