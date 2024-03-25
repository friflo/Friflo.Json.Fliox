// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // --------------------------------- read a single entity ---------------------------------
    public sealed class FindTask<TKey, T> : ReadTaskBase<TKey, T> where T : class
    {
        internal            T           result;
        internal            TKey        key;
        
        public              T           Result      => IsOk("FindTask.Result", out Exception e) ? result : throw e;
        public   override   string      Details     => $"FindTask<{typeof(T).Name}> (id: {key})";

        internal FindTask(Set<TKey, T> set, TKey key) : base (set) {
            relations       = new Relations(this);
            this.key        = key;
        }
        
        protected internal override void Reuse() {
            result      = null;
            key         = default;
            relations   = default;
            state       = default;
            taskName    = null;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return set.ReadEntity(this);
        }
    }

    // --------------------------------- read multiple entities ---------------------------------
    public sealed class ReadTask<TKey, T> : ReadTaskBase<TKey, T> where T : class
    {
        internal readonly   Dictionary<TKey, T>         result      = Set.CreateDictionary<TKey,T>();
        internal readonly   List<FindFunction<TKey, T>> findTasks   = new List<FindFunction<TKey, T>>();
        
        public              Dictionary<TKey, T>         Result      => IsOk("ReadTask.Result", out Exception e) ? result      : throw e;
        public   override   string                      Details     => $"ReadTask<{typeof(T).Name}> (ids: {result.Count})";

        
        internal ReadTask(Set<TKey, T> set) : base (set) {
            relations = new Relations(this);
        }

        public Find<TKey, T> Find(TKey key) {
            if (key == null)
                throw new ArgumentException($"ReadTask.Find() key must not be null. EntitySet: {set.name}");
            if (State.IsExecuted())
                throw AlreadySyncedError();
            result.Add(key, null);
            var find = new Find<TKey, T>(key);
            findTasks.Add(find);
            // set.client.AddFunction(find);
            return find;
        }
        
        public FindRange<TKey, T> FindRange(List<TKey> keys) {
            if (keys == null)
                throw new ArgumentException($"ReadTask.FindRange() keys must not be null. EntitySet: {set.name}");
            if (State.IsExecuted())
                throw AlreadySyncedError();
            result.EnsureCapacity(result.Count + keys.Count);
            foreach (var id in keys) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() key must not be null. EntitySet: {set.name}");
                result.TryAdd(id, null);
            }
            var find = new FindRange<TKey, T>(keys);
            findTasks.Add(find);
            // set.client.AddFunction(find);
            return find;
        }
        
        public FindRange<TKey, T> FindRange(ICollection<TKey> keys) {
            if (keys == null)
                throw new ArgumentException($"ReadTask.FindRange() keys must not be null. EntitySet: {set.name}");
            if (State.IsExecuted())
                throw AlreadySyncedError();
            result.EnsureCapacity(result.Count + keys.Count);
            foreach (var id in keys) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() key must not be null. EntitySet: {set.name}");
                result.TryAdd(id, null);
            }
            var find = new FindRange<TKey, T>(keys);
            findTasks.Add(find);
            // set.client.AddFunction(find);
            return find;
        }
        
        protected internal override void Reuse() {
            result.Clear();
            findTasks.Clear();
            relations   = default;
            state       = default;
            taskName    = null;
            set.readBuffer.Add(this);
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return set.ReadEntities(this);
        }
    }
    
    // --------------------------------- base class for ReadTask<,> & FindTask<,> ---------------------------------
    public abstract class ReadTaskBase <TKey, T> : SyncTask, IRelationsParent, IReadRelationsTask<T> where T : class
    {
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        [DebuggerBrowsable(Never)]
        internal readonly   Set<TKey, T>    set;
        internal            Relations       relations;
        internal override   TaskState       State       => state;
        public              string          Label       => taskName ?? Details;
        internal override   TaskType        TaskType    => TaskType.read;
        public              SyncTask        Task        => this;

        internal ReadTaskBase(Set<TKey, T> set) : base(set) {
            relations       = new Relations(this);
            this.set        = set;
        }

        // --- Relation
        public ReadRelation<TRef> ReadRelation<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TRef>(relation.GetInstance(), path);
        }
        
        public ReadRelation<TRef> ReadRelation<TRefKey, TRef> (EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class   where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TRef>(relation.GetInstance(), path);
        }
        
        public ReadRelation<TRef> ReadRelation<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return ReadRefByPath<TRef>(relation.GetInstance(), selector.path);
        }
        
        
        // --- IReadRelationsTask<T>
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, set.client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, set.client, this);
        }
       
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, set.client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class  where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByExpression<TRef>(relation.GetInstance(), selector, set.client, this);
        }
        
        public ReadRelations<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return relations.ReadRelationsByPath<TRef>(relation.GetInstance(), selector.path, set.client, this);
        }


        // lab - ReadRefs by Entity Type
        /*
        public ReadRefsTask<TRef> ReadRefsOfType<TRef>() where TRef : class {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public ReadRefsTask<object> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        } */
        
        private ReadRelation<TRef> ReadRefByPath<TRef>(Set relation, string path) where TRef : class {
            if (relations.subRelations.TryGetTask(path, out ReadRelationsFunction readRelationsFunction))
                return (ReadRelation<TRef>)readRelationsFunction;
            // var relation = set.client._intern.GetSetByType(typeof(TRef));
            var readRelation    = new ReadRelation<TRef>(this, path, relation, set.client);
            relations.subRelations.AddReadRelations(path, readRelation);
            // set.client.AddFunction(readRelation);
            return readRelation;
        }
    }
}
