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
            return DependencyByPathIntern<TValue>(selector);
        }
        
        public ReadRefs<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            return DependenciesByPathIntern<TValue>(selector);
        }
        
        public ReadRef<TValue> ReadRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (isArraySelector)
                throw new InvalidOperationException($"selector returns an array of dependencies. Use ${nameof(ReadRefs)}()");
            return DependencyByPathIntern<TValue>(path);
        }
        
        public ReadRefs<TValue> ReadRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (!isArraySelector)
                throw new InvalidOperationException($"selector returns a single dependency. Use ${nameof(ReadRef)}()");
            return DependenciesByPathIntern<TValue>(path);
        }

        // lab - dependencies by Entity Type
        public ReadRefs<TValue> DependenciesOfType<TValue>() where TValue : Entity {
            throw new NotImplementedException("DependenciesOfType() planned to be implemented");
        }
        
        // lab - all dependencies
        public ReadRefs<Entity> AllDependencies()
        {
            throw new NotImplementedException("AllDependencies() planned to be implemented");
        }
        
        private ReadRef<TValue> DependencyByPathIntern<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw new InvalidOperationException($"Read already synced. Type: {typeof(T).Name}, id: {id}");
            
            var readDeps = set.GetReadDeps<TValue>(selector);
            if (readDeps.readRefs.TryGetValue(id, out ReadRef readRef))
                return (ReadRef<TValue>)readRef;
            ReadRef<TValue> newDependency = new ReadRef<TValue>(id, set, selector);
            readDeps.readRefs.Add(id, newDependency);
            return newDependency;
        }
        
        private ReadRefs<TValue> DependenciesByPathIntern<TValue>(string selector) where TValue : Entity {
            if (synced)
                throw new InvalidOperationException($"Read already synced. Type: {typeof(T).Name}, id: {id}");
            
            var readDeps = set.GetReadDeps<TValue>(selector);
            if (readDeps.readRefs.TryGetValue(id, out ReadRef readRef))
                return (ReadRefs<TValue>)readRef;
            ReadRefs<TValue> newDependency = new ReadRefs<TValue>(id, set, selector);
            readDeps.readRefs.Add(id, newDependency);
            return newDependency;
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

    
    
    // ----------------------------------------- Dependency<> -----------------------------------------
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
            return new PeerNotSyncedException($"Dependency not synced: {ToString()}");
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
            return new PeerNotSyncedException($"Dependencies not synced: {ToString()}");
        }
    }
    
    internal class ReadDeps
    {
        internal readonly   string                          selector;
        internal readonly   Type                            entityType;
        internal readonly   Dictionary<string, ReadRef>     readRefs = new Dictionary<string, ReadRef>();
        
        internal ReadDeps(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}

