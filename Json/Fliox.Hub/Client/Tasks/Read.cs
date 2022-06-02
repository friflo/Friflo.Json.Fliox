// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyRef;
using Friflo.Json.Fliox.Hub.Protocol.Models;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    
    public abstract  class FindTask<TKey, T> : SyncTask where T : class {
        internal                    TaskState   findState;
        
        public   abstract override  string      Details { get; }
        internal abstract override  TaskState   State   { get; }

        internal abstract void SetFindResult(Dictionary<TKey, T> values, Dictionary<JsonKey, EntityValue> entities, List<TKey> buf);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class Find<TKey, T> : FindTask<TKey, T> where T : class
    {
        private  readonly   TKey        key;
        private             T           result;

        public              T           Result      => IsOk("Find.Result", out Exception e) ? result : throw e;
        internal override   TaskState   State       => findState;
        public   override   string      Details     => $"Find<{typeof(T).Name}> (id: '{key}')";
        
        private static readonly     RefKey<TKey>  KeyConvert = RefKey.GetRefKey<TKey>();
        
        internal Find(TKey key) {
            this.key     = key;
        }
        
        internal override void SetFindResult(Dictionary<TKey, T> values, Dictionary<JsonKey, EntityValue> entities, List<TKey> keysBuf) {
            TaskErrorInfo error = new TaskErrorInfo();
            var id          = KeyConvert.KeyToId(key);
            var entityError = entities[id].Error;
            if (entityError == null) {
                findState.Executed = true;
                result = values[key];
                return;
            }
            error.AddEntityError(entityError);
            findState.SetError(error);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class FindRange<TKey, T> : FindTask<TKey, T> where T : class
    {
        private  readonly   Dictionary<TKey, T>     results;

        public              T                       this[TKey key]      => IsOk("FindRange[]", out Exception e) ? results[key] : throw e;
        public              Dictionary<TKey, T>     Result { get {
            if (IsOk("FindRange.Result", out Exception e))
                return results;
            throw e;
        } }

        internal override   TaskState       State       => findState;
        public   override   string          Details     => $"FindRange<{typeof(T).Name}> (#ids: {results.Count})";
        
        private static readonly     RefKey<TKey>  KeyConvert = RefKey.GetRefKey<TKey>();
        
        internal FindRange(ICollection<TKey> keys) {
            results = SyncSet.CreateDictionary<TKey, T>(keys.Count);
            foreach (var key in keys) {
                results.TryAdd(key, null);
            }
        }
        
        internal override void SetFindResult(Dictionary<TKey, T> values, Dictionary<JsonKey, EntityValue> entities, List<TKey> keysBuf) {
            TaskErrorInfo error = new TaskErrorInfo();
            keysBuf.Clear();
            foreach (var result in results) {
                var key = result.Key;
                var id  = KeyConvert.KeyToId(key);
                var entityError = entities[id].Error;
                if (entityError == null) {
                    keysBuf.Add(key);
                } else {
                    error.AddEntityError(entityError);
                }
            }
            foreach (var key in keysBuf) {
                results[key] = values[key];
            }
            if (error.HasErrors) {
                findState.SetError(error);
                return;
            }
            findState.Executed = true;
        }
    } 
    
    
    // ----------------------------------------- ReadTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ReadTask<TKey, T> : SyncTask, IReadRefsTask<T> where T : class
    {
        internal            TaskState               state;
        internal readonly   EntitySet<TKey, T>      set;
        internal            RefsTask                refsTask;
        internal readonly   Dictionary<TKey, T>     result      = SyncSet.CreateDictionary<TKey,T>();
        internal readonly   List<FindTask<TKey, T>> findTasks   = new List<FindTask<TKey, T>>();

        public              Dictionary<TKey, T>     Result          => IsOk("ReadTask.Result", out Exception e) ? result      : throw e;
        public              T                       this[TKey key]  => IsOk("ReadTask[]",      out Exception e) ? result[key] : throw e;

        internal override   TaskState               State       => state;
        public   override   string                  Details     => $"ReadTask<{typeof(T).Name}> (#ids: {result.Count})";
        

        internal ReadTask(EntitySet<TKey, T> set) {
            refsTask    = new RefsTask(this);
            this.set    = set;
        }

        public Find<TKey, T> Find(TKey key) {
            if (key == null)
                throw new ArgumentException($"ReadTask.Find() id must not be null. EntitySet: {set.name}");
            if (State.IsExecuted())
                throw AlreadySyncedError();
            result.Add(key, null);
            var find = new Find<TKey, T>(key);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
        }
        
        public FindRange<TKey, T> FindRange(ICollection<TKey> keys) {
            if (keys == null)
                throw new ArgumentException($"ReadTask.FindRange() ids must not be null. EntitySet: {set.name}");
            if (State.IsExecuted())
                throw AlreadySyncedError();
            result.EnsureCapacity(result.Count + keys.Count);
            foreach (var id in keys) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() id must not be null. EntitySet: {set.name}");
                result.TryAdd(id, null);
            }
            var find = new FindRange<TKey, T>(keys);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
        }
        
        // --- Relation
        public ReadRefTask<TRefKey, TRef> ReadRelation<TRefKey, TRef>(
            EntitySet<TRefKey, TRef>        relation,
            Expression<Func<T, TRefKey>>    selector) where TRef : class
        {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TRefKey, TRef>(relation, path);
        }
        
        // --- Relations
        public ReadRefsTask<TRefKey, TRef> ReadRelations<TRefKey, TRef>(
            EntitySet<TRefKey, TRef>                    relation,
            Expression<Func<T, IEnumerable<TRefKey>>>   selector) where TRef : class
        {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(relation, selector, set.intern.store);
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
        
        // --- Ref
        public ReadRefTask<TRefKey, TRef> ReadRef<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            var relation = set.intern.store._intern.GetSetByType(typeof(TRef));
            return ReadRefByPath<TRefKey, TRef>(relation, path);
        }
        
        public ReadRefTask<TRefKey, TRef> ReadRefPath<TRefKey, TRef>(RefPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = set.intern.store._intern.GetSetByType(typeof(TRef));
            return ReadRefByPath<TRefKey, TRef>(relation, selector.path);
        }
        
        private ReadRefTask<TRefKey, TRef> ReadRefByPath<TRefKey, TRef>(EntitySet relation, string path) where TRef : class {
            if (refsTask.subRefs.TryGetTask(path, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TRefKey, TRef>)subRefsTask;
            // var relation = set.intern.store._intern.GetSetByType(typeof(TRef));
            var keyName     = relation.GetKeyName();
            var isIntKey    = relation.IsIntKey();
            var newQueryRefs = new ReadRefTask<TRefKey, TRef>(this, path, relation.name, keyName, isIntKey, set.intern.store);
            refsTask.subRefs.AddTask(path, newQueryRefs);
            set.intern.store.AddTask(newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TRefKey, TRef> ReadRefsPath<TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation    = set.intern.store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByPath<TRefKey, TRef>(relation, selector.path, set.intern.store);
        }

        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = set.intern.store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(relation, selector, set.intern.store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey, TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsExecuted())
                throw AlreadySyncedError();
            var relation = set.intern.store._intern.GetSetByType(typeof(TRef));
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(relation, selector, set.intern.store);
        }
        
        internal override void AddFailedTask(List<SyncTask> failed) {
            /*foreach (var findTask in findTasks) {
                if (findTask.State.Error.HasErrors) {
                    failed.Add(findTask);
                }
            }*/
        }
    }
}
