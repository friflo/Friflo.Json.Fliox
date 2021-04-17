// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.EntityGraph
{
    // =======================================   CRUD   ==========================================
    
    // ----------------------------------------- Read<> -----------------------------------------
    public class Read<T> where T : Entity
    {
        private  readonly   string          id;
        internal            T               result;
        internal            bool            synced;
        private  readonly   EntitySet<T>    set;

        public              T               Result      => synced ? result : throw Error();
        public   override   string          ToString()  => id;

        internal Read(string id, EntitySet<T> set) {
            this.id = id;
            this.set = set;
        }

        private Exception Error() {
            return new PeerNotSyncedException($"Read().Result requires Sync(). Entity: {typeof(T).Name} id: {id}");
        }
        
        public ReadRef<TValue> ReadRefByPath<TValue>(string selector) where TValue : Entity {
            return ReadRefByPathIntern<TValue>(selector);
        }
        
        public ReadRefs<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            return ReadRefsByPathIntern<TValue>(selector);
        }
        
        public ReadRef<TValue> ReadRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (isArraySelector)
                throw new InvalidOperationException($"selector returns an array of ReadRefs. Use ${nameof(ReadRefs)}()");
            return ReadRefByPathIntern<TValue>(path);
        }
        
        public ReadRefs<TValue> ReadRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (!isArraySelector)
                throw new InvalidOperationException($"selector returns a single ReadRef. Use ${nameof(ReadRef)}()");
            return ReadRefsByPathIntern<TValue>(path);
        }

        // lab - ReadRefs by Entity Type
        public ReadRefs<TValue> ReadRefsOfType<TValue>() where TValue : Entity {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public ReadRefs<Entity> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        }
        
        private ReadRef<TValue> ReadRefByPathIntern<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw new InvalidOperationException($"Read already synced. Type: {typeof(T).Name}, id: {id}");
            
            var map = set.GetReadRefMap<TValue>(selector);
            if (map.readRefs.TryGetValue(id, out ReadRef readRef))
                return (ReadRef<TValue>)readRef;
            ReadRef<TValue> newReadRef = new ReadRef<TValue>(id, set, selector);
            map.readRefs.Add(id, newReadRef);
            return newReadRef;
        }
        
        private ReadRefs<TValue> ReadRefsByPathIntern<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw new InvalidOperationException($"Read already synced. Type: {typeof(T).Name}, id: {id}");
            
            var map = set.GetReadRefMap<TValue>(selector);
            if (map.readRefs.TryGetValue(id, out ReadRef readRef))
                return (ReadRefs<TValue>)readRef;
            ReadRefs<TValue> newReadRefs = new ReadRefs<TValue>(id, set, selector);
            map.readRefs.Add(id, newReadRefs);
            return newReadRefs;
        }
    }

    public class ReadWhere<T> where T : Entity
    {
        private readonly    List<ReadRef<T>>    results = new List<ReadRef<T>>();

        public              List<ReadRef<T>>    Results => results;
    }
    
    
    // ----------------------------------------- Create<> -----------------------------------------
    public class Create<T> where T : Entity
    {
        private readonly    T           entity;
        private readonly    EntityStore store;

        internal            T           Entity      => entity;
        public   override   string      ToString()  => entity.id;
        
        internal Create(T entity, EntityStore entityStore) {
            this.entity = entity;
            this.store = entityStore;
        }

        // public T Result  => entity;
    }

    
    
    // ----------------------------------------- ReadRef -----------------------------------------
    public class ReadRef
    {
        internal readonly   string      parentId;
        internal readonly   EntitySet   parentSet;
        internal readonly   bool        singleResult;
        internal readonly   string      label;

        public   override   string      ToString() => $"{parentSet.Type.Name}['{parentId}'] {label}";
        
        internal ReadRef(string parentId, EntitySet parentSet, string label, bool singleResult) {
            this.parentId           = parentId;
            this.parentSet          = parentSet;
            this.singleResult       = singleResult;
            this.label              = label;
        }
    }
    
    public class ReadRef<T> : ReadRef where T : Entity
    {
        internal    string      id;
        internal    T           entity;
        internal    bool        synced;

        public      string      Id      => synced ? id      : throw Error();
        public      T           Result  => synced ? entity  : throw Error();

        internal ReadRef(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, true) { }
        
        private Exception Error() {
            return new PeerNotSyncedException($"ReadRef not synced: {ToString()}");
        }
    }
    
    public class ReadRefs<T> : ReadRef where T : Entity
    {
        internal            bool                synced;
        internal readonly   List<ReadRef<T>>    results = new List<ReadRef<T>>();
        
        public              List<ReadRef<T>>    Results         => synced ? results         : throw Error();
        public              ReadRef<T>          this[int index] => synced ? results[index]  : throw Error();

        internal ReadRefs(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, false) { }
        
        private Exception Error() {
            return new PeerNotSyncedException($"ReadRefs not synced: {ToString()}");
        }
    }
    
    internal class ReadRefMap
    {
        internal readonly   string                          selector;
        internal readonly   Type                            entityType;
        internal readonly   Dictionary<string, ReadRef>     readRefs = new Dictionary<string, ReadRef>();
        
        internal ReadRefMap(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}

