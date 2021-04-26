// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- ReadTask -----------------------------------------
    public class ReadTask<T> : RefsTask<T> where T : Entity
    {
        private  readonly   string          id;
        internal readonly   PeerEntity<T>   peer;
        internal            T               result;

        public              T               Result      => synced ? result : throw RequiresSyncError("ReadTask.Result requires Sync().");
        
        public   override   string          Label       => $"ReadTask<{typeof(T).Name}> id: {id}";
        public   override   string          ToString()  => Label;

        internal ReadTask(string id, PeerEntity<T> peer) {
            this.id     = id;
            this.peer   = peer;
        }

        // lab - ReadRefs by Entity Type
        public SubRefsTask<TValue> ReadRefsOfType<TValue>() where TValue : Entity {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public SubRefsTask<Entity> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        }
        
        public SubRefTask<TValue> SubRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out _);
            return SubRefByPath<TValue>(path);
        }
        
        public SubRefTask<TValue> SubRefByPath<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            if (subRefs.TryGetValue(selector, out ISubRefsTask subRefsTask))
                return (SubRefTask<TValue>)subRefsTask;
            var newQueryRefs = new SubRefTask<TValue>(this, selector, typeof(TValue).Name);
            subRefs.Add(selector, newQueryRefs);
            return newQueryRefs;
        }
    }
}
