// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class Find<T> : SyncTask where T : Entity
    {
        private  readonly   string      id;
        internal readonly   ReadTask<T> task; 

        public              T           Result      => IsOk("Find.Result", out Exception e) ? task.idMap[id] : throw e;

        internal override   TaskState   State       => task.State;
        
        internal override   string      Label       => $"ReadId<{typeof(T).Name}> id: {id}";
        public   override   string      ToString()  => Label;

        internal Find(ReadTask<T> task, string id) {
            this.id     = id;
            this.task   = task;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class FindRange<T> : SyncTask where T : Entity
    {
        private  readonly   HashSet<string>         ids;
        private  readonly   ReadTask<T>             task; 

        public              T                       this[string id]      => IsOk("FindRange[]", out Exception e) ? task.idMap[id] : throw e;
        public              Dictionary<string, T>   Results { get {
            if (!IsOk("FindRange.Results", out Exception e)) {
                throw e;
            }
            var result = new Dictionary<string, T>(ids.Count);
            foreach (var id in ids) {
                result.Add(id, task.idMap[id]);
            }
            return result;
        } }

        internal override   TaskState   State       => task.State;
        internal override   string      Label       => $"ReadIds<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string      ToString()  => Label;

        internal FindRange(ReadTask<T> task, ICollection<string> ids) {
            this.ids    = ids.ToHashSet();
            this.task   = task;
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
        internal readonly   Dictionary<string, T>   idMap = new Dictionary<string, T>();
        
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
            return new Find<T>(this, id);
        }
        
        public FindRange<T> FindRange(ICollection<string> ids) {
            if (ids == null)
                throw new ArgumentException($"ReadTask.FindRange() ids must not be null. EntitySet: {set.name}");
            if (State.IsSynced())
                throw AlreadySyncedError();
#if !UNITY_5_3_OR_NEWER
            idMap.EnsureCapacity(idMap.Count + ids.Count);
#endif
            foreach (var id in ids) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() id must not be null. EntitySet: {set.name}");
                idMap.TryAdd(id, null);
            }
            return new FindRange<T>(this, ids);
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
        
        public ReadRefTask<TRef> Read<TRef>(RefPath<T, TRef> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return ReadRefByPath<TRef>(selector.path);
        }
        
        private ReadRefTask<TRef> ReadRefByPath<TRef>(string path) where TRef : Entity {
            if (refsTask.subRefs.TryGetTask(path, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TRef>)subRefsTask;
            var newQueryRefs = new ReadRefTask<TRef>(this, path, typeof(TRef).Name);
            refsTask.subRefs.AddTask(path, newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TRef> Read<TRef>(RefsPath<T, TRef> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(selector.path);
        }

        public ReadRefsTask<TRef> ReadRefs<TRef>(Expression<Func<T, Ref<TRef>>> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector);
        }
        
        public ReadRefsTask<TRef> ReadArrayRefs<TRef>(Expression<Func<T, IEnumerable<Ref<TRef>>>> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector);
        }
    }
}
