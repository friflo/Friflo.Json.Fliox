// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    
    public abstract  class FindTask<TKey, T> : SyncTask where T : class {
        internal                    TaskState   findState;
        
        public   abstract override  string      Details { get; }
        internal abstract override  TaskState   State   { get; }

        internal abstract void SetFindResult(Dictionary<TKey, T> values, Dictionary<string, EntityValue> entities);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class Find<TKey, T> : FindTask<TKey, T> where T : class
    {
        private  readonly   TKey        key;
        private             T           result;

        public              T           Result      => IsOk("Find.Result", out Exception e) ? result : throw e;
        internal override   TaskState   State       => findState;
        public   override   string      Details     => $"Find<{typeof(T).Name}> (id: '{key}')";
        

        internal Find(TKey key) {
            this.key     = key;
        }
        
        internal override void SetFindResult(Dictionary<TKey, T> values, Dictionary<string, EntityValue> entities) {
            TaskErrorInfo error = new TaskErrorInfo();
            var id = Ref<TKey,T>.EntityKey.KeyToId(key);
            var entityError = entities[id].Error;
            if (entityError == null) {
                findState.Synced = true;
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
    public class FindRange<TKey, T> : FindTask<TKey, T> where T : class
    {
        private  readonly   HashSet<TKey>           keys;
        private  readonly   Dictionary<TKey, T>     results = new Dictionary<TKey, T>();

        public              T                       this[TKey key]      => IsOk("FindRange[]", out Exception e) ? results[key] : throw e;
        public              Dictionary<TKey, T>     Results { get {
            if (IsOk("FindRange.Results", out Exception e))
                return results;
            throw e;
        } }

        internal override   TaskState       State       => findState;
        public   override   string          Details     => $"FindRange<{typeof(T).Name}> (#ids: {keys.Count})";
        

        internal FindRange(ICollection<TKey> keys) {
            this.keys    = keys.ToHashSet();
        }
        
        internal override void SetFindResult(Dictionary<TKey, T> values, Dictionary<string, EntityValue> entities) {
            TaskErrorInfo error = new TaskErrorInfo();
            foreach (var key in keys) {
                var id = Ref<TKey,T>.EntityKey.KeyToId(key);
                var entityError = entities[id].Error;
                if (entityError == null) {
                    results.Add(key, values[key]);    
                } else {
                    error.AddEntityError(entityError);
                }
            }
            if (error.HasErrors) {
                findState.SetError(error);
                return;
            }
            findState.Synced = true;
        }
    } 
    
    
    // ----------------------------------------- ReadTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ReadTask<TKey, T> : SyncTask, IReadRefsTask<T> where T : class
    {
        internal            TaskState               state;
        internal readonly   EntityPeerSet<T>        set;
        internal            RefsTask                refsTask;
        internal readonly   Dictionary<TKey, T>     results     = new Dictionary<TKey, T>();
        internal readonly   List<FindTask<TKey, T>> findTasks   = new List<FindTask<TKey, T>>();

        public              Dictionary<TKey, T>     Results         => IsOk("ReadTask.Results", out Exception e) ? results      : throw e;
        public              T                       this[TKey key]  => IsOk("ReadTask[]",       out Exception e) ? results[key] : throw e;

        internal override   TaskState               State       => state;
        public   override   string                  Details     => $"ReadTask<{typeof(T).Name}> (#ids: {results.Count})";
        

        internal ReadTask(EntityPeerSet<T> set) {
            refsTask    = new RefsTask(this);
            this.set    = set;
        }

        public Find<TKey, T> Find(TKey key) {
            if (key == null)
                throw new ArgumentException($"ReadTask.Find() id must not be null. EntitySet: {set.name}");
            if (State.IsSynced())
                throw AlreadySyncedError();
            results.Add(key, null);
            var find = new Find<TKey, T>(key);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
        }
        
        public FindRange<TKey, T> FindRange(ICollection<TKey> keys) {
            if (keys == null)
                throw new ArgumentException($"ReadTask.FindRange() ids must not be null. EntitySet: {set.name}");
            if (State.IsSynced())
                throw AlreadySyncedError();
            results.EnsureCapacity(results.Count + keys.Count);
            foreach (var id in keys) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() id must not be null. EntitySet: {set.name}");
                results.TryAdd(id, null);
            }
            var find = new FindRange<TKey, T>(keys);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
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
        public ReadRefTask<TRef> ReadRef<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TRef>(path);
        }
        
        public ReadRefTask<TRef> ReadRefPath<TRefKey, TRef>(RefPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return ReadRefByPath<TRef>(selector.path);
        }
        
        private ReadRefTask<TRef> ReadRefByPath<TRef>(string path) where TRef : class {
            if (refsTask.subRefs.TryGetTask(path, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TRef>)subRefsTask;
            var newQueryRefs = new ReadRefTask<TRef>(this, path, typeof(TRef).Name, set.intern.store);
            refsTask.subRefs.AddTask(path, newQueryRefs);
            set.intern.store.AddTask(newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TRefKey, TRef> ReadRefsPath<TRefKey, TRef>(RefsPath<T, TRefKey, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRefKey, TRef>(selector.path, set.intern.store);
        }

        public ReadRefsTask<TRefKey, TRef> ReadRefs<TRefKey, TRef>(Expression<Func<T, Ref<TRefKey, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, set.intern.store);
        }
        
        public ReadRefsTask<TRefKey, TRef> ReadArrayRefs<TRefKey, TRef>(Expression<Func<T, IEnumerable<Ref<TRefKey, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRefKey, TRef>(selector, set.intern.store);
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
