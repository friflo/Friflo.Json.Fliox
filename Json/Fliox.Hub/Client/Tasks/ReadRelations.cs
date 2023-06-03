// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{

    // could be an interface, but than internal used methods would be public (C# 8.0 enables internal interface methods) 
    public abstract class ReadRelationsFunction : SyncFunction
    {
        [DebuggerBrowsable(Never)]
        internal            TaskState   state;
        internal abstract   string      Selector    { get; }
        internal abstract   ShortString Container   { get; }
        internal abstract   string      KeyName     { get; }
        internal abstract   bool        IsIntKey    { get; }
        internal abstract   SubRelations SubRelations     { get; }
        
        internal abstract void    SetResult (EntitySet set, List<JsonKey> ids);
    }
    
    public abstract class ReadRelationsFunction<T> : ReadRelationsFunction, IReadRelationsTask<T> where T : class
    {
        internal            Relations       relations;
        private   readonly  FlioxClient     client;
        
        protected ReadRelationsFunction(FlioxClient client) {
            relations   = new Relations(this);
            this.client = client;
        }

        // --- IReadRelationsTask<T>
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByPath<TRef>(relation.GetInstance(), selector.path, client);
        }
    }

    /// ensure all tasks returning <see cref="ReadRelations{T}"/>'s provide the same interface
    public interface IReadRelationsTask<T> where T : class
    {
        ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>>              selector) where TRef : class;
        ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>>             selector) where TRef : class  where TRefKey : struct;
        ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class;
        ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>>selector) where TRef : class  where TRefKey : struct;
        ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef>                       selector) where TRef : class;
    }

    // ----------------------------------------- ReadRefsTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ReadRelations<T> : ReadRelationsFunction<T>  where T : class
    {
        private             List<T>         result;
        private   readonly  SyncFunction    parent;
            
        public              List<T>     Result  => IsOk("ReadRelations.Result", out Exception e) ? result      : throw e;
        
        internal  override  TaskState   State   => state;
        public    override  string      Details => $"{parent.GetLabel()} -> {Selector}";
            
        internal  override  string      Selector  { get; }
        internal  override  ShortString Container { get; }
        internal  override  string      KeyName   { get; }
        internal  override  bool        IsIntKey  { get; }
        
        internal  override  SubRelations SubRelations => relations.subRelations;


        internal ReadRelations(SyncFunction parent, string selector, in ShortString container, string keyName, bool isIntKey, FlioxClient client)
            : base(client)
        {
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
            this.KeyName    = keyName;
            this.IsIntKey   = isIntKey;
        }

        internal override void SetResult(EntitySet set, List<JsonKey> ids) {
            var entitySet = (EntitySetBase<T>) set;
            result = new List<T>(ids.Count);
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                if (peer.error == null) {
                    result.Add(peer.Entity);
                } else {
                    entityErrorInfo.AddEntityError(peer.error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            }
        }
    }
}
