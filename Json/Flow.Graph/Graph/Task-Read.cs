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
    
    public abstract  class FindTask<T> : SyncTask where T : Entity {
        internal                    TaskState   findState;
        
        internal abstract override  string      Label { get; }
        internal abstract override  TaskState   State { get; }

        internal abstract void SetFindResult(Dictionary<string, T> values, Dictionary<string, EntityValue> entities);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class Find<T> : FindTask<T> where T : Entity
    {
        private  readonly   string      id;
        private             T           result;

        public              T           Result      => IsOk("Find.Result", out Exception e) ? result : throw e;

        internal override   TaskState   State       => findState;

        internal override   string      Label       => $"ReadId<{typeof(T).Name}> id: {id}";
        public   override   string      ToString()  => Label;

        internal Find(string id) {
            this.id     = id;
        }
        
        internal override void SetFindResult(Dictionary<string, T> values, Dictionary<string, EntityValue> entities) {
            TaskErrorInfo error = new TaskErrorInfo();
            var entityError = entities[id].Error;
            if (entityError == null) {
                findState.Synced = true;
                result = values[id];
                return;
            }
            error.AddEntityError(entityError);
            findState.SetError(error);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class FindRange<T> : FindTask<T> where T : Entity
    {
        private  readonly   HashSet<string>         ids;
        private  readonly   Dictionary<string, T>   results = new Dictionary<string, T>();

        public              T                       this[string id]      => IsOk("FindRange[]", out Exception e) ? results[id] : throw e;
        public              Dictionary<string, T>   Results { get {
            if (IsOk("FindRange.Results", out Exception e))
                return results;
            throw e;
        } }

        internal override   TaskState   State       => findState;
        internal override   string      Label       => $"ReadIds<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string      ToString()  => Label;

        internal FindRange(ICollection<string> ids) {
            this.ids    = ids.ToHashSet();
        }
        
        internal override void SetFindResult(Dictionary<string, T> values, Dictionary<string, EntityValue> entities) {
            TaskErrorInfo error = new TaskErrorInfo();
            foreach (var id in ids) {
                var entityError = entities[id].Error;
                if (entityError == null) {
                    results.Add(id, values[id]);    
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
    public class ReadTask<T> : SyncTask, IReadRefsTask<T> where T : Entity
    {
        internal            TaskState               state;
        internal readonly   EntitySet<T>            set;
        internal            RefsTask                refsTask;
        internal readonly   Dictionary<string, T>   idMap       = new Dictionary<string, T>();
        internal readonly   List<FindTask<T>>       findTasks   = new List<FindTask<T>>();

        public              Dictionary<string, T>   Results          => IsOk("ReadTask.Results", out Exception e) ? idMap     : throw e;
        public              T                       this[string id]  => IsOk("ReadTask[]",       out Exception e) ? idMap[id] : throw e;

        internal override   TaskState               State       => state;
        internal override   string                  Label       => $"ReadTask<{typeof(T).Name}> #ids: {idMap.Count}";
        public   override   string                  ToString()  => Label;

        internal ReadTask(EntitySet<T> set) {
            refsTask    = new RefsTask(this);
            this.set    = set;
        }

        public Find<T> Find(string id) {
            if (id == null)
                throw new ArgumentException($"ReadTask.Find() id must not be null. EntitySet: {set.name}");
            if (State.IsSynced())
                throw AlreadySyncedError();
            idMap.Add(id, null);
            var find = new Find<T>(id);
            findTasks.Add(find);
            return find;
        }
        
        public FindRange<T> FindRange(ICollection<string> ids) {
            if (ids == null)
                throw new ArgumentException($"ReadTask.FindRange() ids must not be null. EntitySet: {set.name}");
            if (State.IsSynced())
                throw AlreadySyncedError();
            idMap.EnsureCapacity(idMap.Count + ids.Count);
            foreach (var id in ids) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() id must not be null. EntitySet: {set.name}");
                idMap.TryAdd(id, null);
            }
            var find = new FindRange<T>(ids);
            findTasks.Add(find);
            return find;
        }

        // lab - ReadRefs by Entity Type
        public ReadRefsTask<TRef> ReadRefsOfType<TRef>() where TRef : Entity {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public ReadRefsTask<Entity> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        }
        
        // --- Ref
        public ReadRefTask<TRef> ReadRef<TRef>(Expression<Func<T, Ref<TRef>>> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TRef>(path);
        }
        
        public ReadRefTask<TRef> ReadRefPath<TRef>(RefPath<T, TRef> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return ReadRefByPath<TRef>(selector.path);
        }
        
        private ReadRefTask<TRef> ReadRefByPath<TRef>(string path) where TRef : Entity {
            if (refsTask.subRefs.TryGetTask(path, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TRef>)subRefsTask;
            var newQueryRefs = new ReadRefTask<TRef>(this, path, typeof(TRef).Name, set.intern.store);
            refsTask.subRefs.AddTask(path, newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TRef> ReadRefsPath<TRef>(RefsPath<T, TRef> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(selector.path, set.intern.store);
        }

        public ReadRefsTask<TRef> ReadRefs<TRef>(Expression<Func<T, Ref<TRef>>> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector, set.intern.store);
        }
        
        public ReadRefsTask<TRef> ReadArrayRefs<TRef>(Expression<Func<T, IEnumerable<Ref<TRef>>>> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector, set.intern.store);
        }
    }
}
