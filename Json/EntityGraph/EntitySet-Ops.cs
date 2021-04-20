// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.EntityGraph
{
    // =======================================   CRUD   ==========================================
    
    // ----------------------------------------- Read<> -----------------------------------------
    public class ReadTask<T> where T : Entity
    {
        private  readonly   string          id;
        internal            T               result;
        internal            bool            synced;
        private  readonly   EntitySet<T>    set;

        public              T               Result      => synced ? result : throw Error();
        public   override   string          ToString()  => id;

        internal ReadTask(string id, EntitySet<T> set) {
            this.id = id;
            this.set = set;
        }

        private Exception Error() {
            return new PeerNotSyncedException($"ReadTask.Result requires Sync(). Entity: {set.type.Name} id: {id}");
        }
        
        public ReadRefTask<TValue> ReadRefByPath<TValue>(string selector) where TValue : Entity {
            return ReadRefByPathIntern<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            return ReadRefsByPathIntern<TValue>(selector);
        }
        
        public ReadRefTask<TValue> ReadRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (isArraySelector)
                throw new InvalidOperationException($"selector returns an array of ReadRefs. Use ${nameof(ReadRefs)}()");
            return ReadRefByPathIntern<TValue>(path);
        }
        
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
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
            if (synced)
                throw new InvalidOperationException($"ReadRefTask already synced. Type: {typeof(T).Name}, id: {id}");
            
            var map = set.tasks.GetReadRefMap<TValue>(selector);
            if (map.readRefs.TryGetValue(id, out ReadRefTask readRef))
                return (ReadRefTask<TValue>)readRef;
            ReadRefTask<TValue> newReadRef = new ReadRefTask<TValue>(id, set, selector);
            map.readRefs.Add(id, newReadRef);
            return newReadRef;
        }
        
        private ReadRefsTask<TValue> ReadRefsByPathIntern<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw new InvalidOperationException($"ReadRefsTask already synced. Type: {typeof(T).Name}, id: {id}");
            
            var map = set.tasks.GetReadRefMap<TValue>(selector);
            if (map.readRefs.TryGetValue(id, out ReadRefTask readRef))
                return (ReadRefsTask<TValue>)readRef;
            ReadRefsTask<TValue> newReadRefs = new ReadRefsTask<TValue>(id, set, selector);
            map.readRefs.Add(id, newReadRefs);
            return newReadRefs;
        }
    }

    public class QueryTask<T> where T : Entity
    {
        internal readonly   FilterOperation filter;
        private  readonly   EntitySet<T>    set;
        internal            bool            synced;
        internal readonly   List<T>         entities = new List<T>();
        
        public              List<T>         Result          => synced ? entities        : throw Error("QueryTask.Result requires Sync().");
        public              T               this[int index] => synced ? entities[index] : throw Error("QueryTask[] requires Sync().");

        public override     string          ToString() => filter.Linq;

        internal QueryTask(FilterOperation filter, EntitySet<T> set) {
            this.filter = filter;
            this.set    = set;
        }
        
        private Exception Error(string message) {
            return new PeerNotSyncedException($"{message} Entity: {set.type.Name} filter: {filter.Linq}");
        }
    }
    
    
    // ----------------------------------------- Create<> -----------------------------------------
    public class CreateTask<T> where T : Entity
    {
        private readonly    T           entity;

        internal            T           Entity      => entity;
        public   override   string      ToString()  => entity.id;
        
        internal CreateTask(T entity) {
            this.entity = entity;
        }

        // public T Result  => entity;
    }


    // ----------------------------------------- ReadRef -----------------------------------------
    public class ReadRefTask
    {
        internal readonly   string      parentId;
        internal readonly   EntitySet   parentSet;
        internal readonly   bool        singleResult;
        internal readonly   string      label;

        private             string      DebugName => $"{parentSet.Type.Name}['{parentId}'] {label}";
        public   override   string      ToString() => DebugName;
        
        internal ReadRefTask(string parentId, EntitySet parentSet, string label, bool singleResult) {
            this.parentId           = parentId;
            this.parentSet          = parentSet;
            this.singleResult       = singleResult;
            this.label              = label;
        }
        
        protected Exception Error(string message) {
            return new PeerNotSyncedException($"{message} {DebugName}");
        }
    }
    
    public class ReadRefTask<T> : ReadRefTask where T : Entity
    {
        internal    string      id;
        internal    T           entity;
        internal    bool        synced;

        public      string      Id      => synced ? id      : throw Error("ReadRefTask.Id requires Sync().");
        public      T           Result  => synced ? entity  : throw Error("ReadRefTask.Result requires Sync().");

        internal ReadRefTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, true) { }
    }
    
    public class ReadRefsTask<T> : ReadRefTask where T : Entity
    {
        internal            bool                    synced;
        internal readonly   List<ReadRefTask<T>>    results = new List<ReadRefTask<T>>();
        
        public              List<ReadRefTask<T>>    Results         => synced ? results         : throw Error("ReadRefsTask.Results requires Sync().");
        public              ReadRefTask<T>          this[int index] => synced ? results[index]  : throw Error("ReadRefsTask[] requires Sync().");

        internal ReadRefsTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, false) { }
    }
    
    internal class ReadRefTaskMap
    {
        internal readonly   string                          selector;
        internal readonly   Type                            entityType;
        internal readonly   Dictionary<string, ReadRefTask> readRefs = new Dictionary<string, ReadRefTask>();
        
        internal ReadRefTaskMap(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}

