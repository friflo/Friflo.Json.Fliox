// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{

    public class ReadId<T> where T : Entity
    {
        private  readonly   string      id;
        internal readonly   ReadTask<T> task; 

        public              T           Result      => task.synced ? task.ids[id] : throw task.RequiresSyncError("ReadId.Result requires Sync().");

        private             string      Label => $"ReadId<{typeof(T).Name}> id: {id}";
        public   override   string      ToString() => Label;

        internal ReadId(ReadTask<T> task, string id) {
            this.id     = id;
            this.task   = task;
        }
    } 
    
    
    // ----------------------------------------- ReadTask -----------------------------------------
    public class ReadTask<T> : EntitySetTask, IReadRefsTask<T> where T : Entity
    {
        internal            bool                    synced;
        internal readonly   EntitySet<T>            set;
        internal            RefsTask                refsTask;
        internal readonly   Dictionary<string, T>   ids = new Dictionary<string, T>();

        internal override   bool                    Synced      => synced;
        internal override   string                  Label       => $"ReadTask<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string                  ToString()  => Label;

        internal ReadTask(EntitySet<T> set) {
            refsTask    = new RefsTask(this);
            this.set    = set;
        }

        public ReadId<T> ReadId(string id) {
            if (id == null)
                throw new InvalidOperationException($"EntitySet.Read() id must not be null. EntitySet: {set.name}");
            if (Synced)
                throw AlreadySyncedError();
            ids.Add(id, null);
            return new ReadId<T>(this, id);
        }

        // lab - ReadRefs by Entity Type
        public ReadRefsTask<TValue> ReadRefsOfType<TValue>() where TValue : Entity {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public ReadRefsTask<Entity> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        }
        
        // --- Refs
        public ReadRefTask<TValue> ReadRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TValue>(path);
        }
        
        public ReadRefTask<TValue> ReadRefByPath<TValue>(string selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            if (refsTask.subRefs.TryGetTask(selector, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TValue>)subRefsTask;
            var newQueryRefs = new ReadRefTask<TValue>(this, selector, typeof(TValue).Name);
            refsTask.subRefs.AddTask(selector, newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadArrayRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
}
