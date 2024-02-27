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

    public interface IRelationsParent
    {
        public  SyncTask    Task            { get; }
        public  string      Label           { get; }
        public  string      Details         { get; }
    }
    
    // could be an interface, but than internal used methods would be public (C# 8.0 enables internal interface methods) 
    /// <summary>originally extended SyncFunction. Its members are now in <see cref="SyncTask"/> </summary>
    public abstract class ReadRelationsFunction : IRelationsParent // : SyncFunction
    {
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal abstract   string          Selector        { get; }
        internal abstract   Set             Relation        { get; }
        internal abstract   SubRelations    SubRelations    { get; }
        public   abstract   SyncTask        Task            { get; }
        public   abstract   string          Label           { get; }
        public   abstract   string          Details         { get; }
        public   override   string          ToString()      => Label;

        
        /// <summary>
        /// Is true in case task execution was successful. Otherwise false. If false <see cref="Error"/> property is set. 
        /// </summary>
        /// <exception cref="TaskNotSyncedException"></exception>
        public              bool        Success { get {
            if (state.IsExecuted())
                return !state.Error.HasErrors;
            throw new TaskNotSyncedException($"SyncTask.Success requires SyncTasks(). {Label}");
        }}
        
        /// <summary>The error caused the task failing. Return null if task was successful - <see cref="Success"/> == true</summary>
        public              TaskError   Error { get {
            if (state.IsExecuted())
                return state.Error.TaskError;
            throw new TaskNotSyncedException($"SyncTask.Error requires SyncTasks(). {Label}");
        } }
        
        internal bool IsOk(string method, out Exception e) {
            if (state.IsExecuted()) {
                if (!state.Error.HasErrors) {
                    e = null;
                    return true;
                }
                e = new TaskResultException(state.Error.TaskError);
                return false;
            }
            e = new TaskNotSyncedException($"{method} requires SyncTasks(). {Label}");
            return false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already executed. {Label}");
        }
        
        internal abstract void    SetResult (Entity[] objects);
    }
    
    public abstract class ReadRelationsFunction<T> : ReadRelationsFunction, IReadRelationsTask<T> where T : class
    {
        internal            Relations       relations;
        private   readonly  FlioxClient     client;
        public    override  SyncTask        Task       => relations.parent.Task;
        
        protected ReadRelationsFunction(FlioxClient client, IRelationsParent parent) {
            relations   = new Relations(parent);
            this.client = client;
        }

        // --- IReadRelationsTask<T>
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (state.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class where TRefKey : struct {
            if (state.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (state.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class where TRefKey : struct {
            if (state.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (state.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByPath<TRef>(relation.GetInstance(), selector.path, client, this);
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
    [CLSCompliant(true)]
    public sealed class ReadRelations<T> : ReadRelationsFunction<T>  where T : class
    {
        private             List<T>             result;
        private   readonly  IRelationsParent    parent;
            
        public              List<T>     Result  => IsOk("ReadRelations.Result", out Exception e) ? result      : throw e;
        
        //internal  override  TaskState   State   => state;
        public    override  string      Label   => $"{parent.Label} -> {Selector}";
        public    override  string      Details => $"{parent.Details} -> {Selector}";
            
        internal  override  string      Selector  { get; }
        internal  override  Set         Relation { get; }
        
        internal  override  SubRelations SubRelations => relations.subRelations;


        internal ReadRelations(IRelationsParent parent, string selector, Set relation, FlioxClient client)
            : base(client, parent)
        {
            this.parent     = parent;
            this.Selector   = selector;
            this.Relation   = relation;
        }

        internal override void SetResult(Entity[] entities)
        {
            result = new List<T>(entities.Length);
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var entity in entities) {
                if (entity.error == null) {
                    result.Add((T)entity.value);
                } else {
                    entityErrorInfo.AddEntityError(entity.error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            }
        }
    }
}
