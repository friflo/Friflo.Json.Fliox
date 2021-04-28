// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- ReadTask -----------------------------------------
    public class ReadTask<T> : ISetTask, IReadRefsTask<T> where T : Entity
    {
        internal            RefsTask        refsTask;
        internal readonly   string          id;
        internal readonly   PeerEntity<T>   peer;
        internal            T               result;

        public              T               Result      => refsTask.synced ? result : throw refsTask.RequiresSyncError("ReadTask.Result requires Sync().");
        
        public              string          Label       => $"ReadTask<{typeof(T).Name}> id: {id}";
        public   override   string          ToString()  => Label;

        internal ReadTask(string id, PeerEntity<T> peer) {
            refsTask    = new RefsTask(this);
            this.id     = id;
            this.peer   = peer;
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
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TValue>(path);
        }
        
        public ReadRefTask<TValue> ReadRefByPath<TValue>(string selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            if (refsTask.subRefs.TryGetTask(selector, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TValue>)subRefsTask;
            var newQueryRefs = new ReadRefTask<TValue>(this, selector, typeof(TValue).Name);
            refsTask.subRefs.AddTask(selector, newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadArrayRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (refsTask.synced)
                throw refsTask.AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
}
