// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- ReadTask -----------------------------------------
    public class ReadTask<T> where T : Entity
    {
        private  readonly   string          id;
        internal readonly   PeerEntity<T>   peer;
        internal            T               result;
        internal            bool            synced;
        private  readonly   EntitySet<T>    set;

        public              T               Result      => synced ? result : throw RequiresSyncError();
        public   override   string          ToString()  => id;

        internal ReadTask(string id, EntitySet<T> set, PeerEntity<T> peer) {
            this.id     = id;
            this.set    = set;
            this.peer   = peer;
        }

        private Exception RequiresSyncError() {
            return new TaskNotSyncedException($"ReadTask.Result requires Sync(). ReadTask<{typeof(T).Name}> id: {id}");
        }

        private Exception AlreadySyncedError() {
            return new InvalidOperationException($"Used ReadTask is already synced. ReadTask<{typeof(T).Name}>, id: {id}");
        }
        
        public ReadRefTask<TValue> ReadRefByPath<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            return ReadRefByPathIntern<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            return ReadRefsByPathIntern<TValue>(selector);
        }
        
        public ReadRefTask<TValue> ReadRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            if (synced)
                throw AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (isArraySelector)
                throw new InvalidOperationException($"selector returns an array of ReadRefs. Use ${nameof(ReadRefs)}()");
            return ReadRefByPathIntern<TValue>(path);
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (synced)
                throw AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (!isArraySelector)
                throw new InvalidOperationException($"selector returns a single ReadRef. Use ${nameof(ReadRef)}()");
            return ReadRefsByPathIntern<TValue>(path);
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
        
        private ReadRefTask<TValue> ReadRefByPathIntern<TValue>(string selector) where TValue : Entity {
            var map = set.sync.GetReadRefMap<TValue>(selector);
            if (map.readRefs.TryGetValue(id, out ReadRefTask readRef))
                return (ReadRefTask<TValue>)readRef;
            var newReadRef = new ReadRefTask<TValue>(id, set, selector);
            map.readRefs.Add(id, newReadRef);
            return newReadRef;
        }
        
        private ReadRefsTask<TValue> ReadRefsByPathIntern<TValue>(string selector) where TValue : Entity {
            var map = set.sync.GetReadRefMap<TValue>(selector);
            if (map.readRefs.TryGetValue(id, out ReadRefTask readRef))
                return (ReadRefsTask<TValue>)readRef;
            var newReadRefs = new ReadRefsTask<TValue>(id, set, selector);
            map.readRefs.Add(id, newReadRefs);
            return newReadRefs;
        }
    }
}
