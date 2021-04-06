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

        internal Read(string id, EntitySet<T> set) {
            this.id = id;
            this.set = set;
        }
            
        public T Result {
            get {
                if (synced)
                    return result;
                throw new PeerNotSyncedException($"Read().Result requires Sync(). Entity: {typeof(T).Name} id: {id}");
            }
        }
        
        public Dependency<TValue> DependencyByPath<TValue>(string selector) where TValue : Entity {
            return DependencyByPathIntern<TValue>(selector);
        }
        
        public Dependencies<TValue> DependenciesByPath<TValue>(string selector) where TValue : Entity {
            return DependenciesByPathIntern<TValue>(selector);
        }
        
        public Dependency<TValue> Dependency<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (isArraySelector)
                throw new InvalidOperationException($"selector returns an array of dependencies. Use ${nameof(Dependencies)}()");
            return DependencyByPathIntern<TValue>(path);
        }
        
        public Dependencies<TValue> Dependencies<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(selector, out bool isArraySelector);
            if (!isArraySelector)
                throw new InvalidOperationException($"selector returns a single dependency. Use ${nameof(Dependency)}()");
            return DependenciesByPathIntern<TValue>(path);
        }

        // lab - dependencies by Entity Type
        public Dependencies<TValue> DependenciesOfType<TValue>() where TValue : Entity {
            throw new NotImplementedException("DependenciesOfType() planned to be implemented");
        }
        
        // lab - all dependencies
        public Dependencies<Entity> AllDependencies()
        {
            throw new NotImplementedException("AllDependencies() planned to be implemented");
        }
        
        private Dependency<TValue> DependencyByPathIntern<TValue>(string selector) where TValue : Entity {
            var readDeps = set.GetReadDeps<TValue>(selector);
            if (readDeps.dependencies.TryGetValue(id, out Dependency dependency))
                return (Dependency<TValue>)dependency;
            Dependency<TValue> newDependency = new Dependency<TValue>(id);
            readDeps.dependencies.Add(id, newDependency);
            return newDependency;
        }
        
        private Dependencies<TValue> DependenciesByPathIntern<TValue>(string selector) where TValue : Entity {
            var readDeps = set.GetReadDeps<TValue>(selector);
            if (readDeps.dependencies.TryGetValue(id, out Dependency dependency))
                return (Dependencies<TValue>)dependency;
            Dependencies<TValue> newDependency = new Dependencies<TValue>(id);
            readDeps.dependencies.Add(id, newDependency);
            return newDependency;
        }
    }
    
    
    // ----------------------------------------- Create<> -----------------------------------------
    public class Create<T> where T : Entity
    {
        private readonly    T           entity;
        private readonly    EntityStore store;

        internal            T           Entity => entity;
        
        internal Create(T entity, EntityStore entityStore) {
            this.entity = entity;
            this.store = entityStore;
        }

        // public T Result  => entity;
    }

    
    
    // ----------------------------------------- Dependency<> -----------------------------------------
    public class Dependency
    {
        internal readonly   string      parentId;
        internal readonly   bool        singleResult;

        internal Dependency(string parentId, bool singleResult) {
            this.parentId       = parentId;
            this.singleResult   = singleResult;
        }
    }
    
    public class Dependency<T> : Dependency where T : Entity
    {
        internal    string      id;
        internal    T           entity;
        internal    bool        synced;

        public      string      Id      => synced ? id      : throw Error();
        public      T           Result  => synced ? entity  : throw Error();

        internal Dependency(string parentId) : base (parentId, true) { }
        
        private Exception Error() {
            return new PeerNotSyncedException($"Dependency not synced. Dependency<{typeof(T).Name}>");
        }
    }
    
    public class Dependencies<T> : Dependency where T : Entity
    {
        internal            bool                synced;
        internal readonly   List<Dependency<T>> results = new List<Dependency<T>>();
        
        public              List<Dependency<T>> Results         => synced ? results         : throw Error();
        public              Dependency<T>       this[int index] => synced ? results[index]  : throw Error();

        internal Dependencies(string parentId) : base (parentId, false) { }
        
        private Exception Error() {
            return new PeerNotSyncedException($"Dependencies not synced. Dependencies<{typeof(T).Name}>");
        }
    }
    
    internal class ReadDeps
    {
        internal readonly   string                          selector;
        internal readonly   Type                            entityType;
        internal readonly   Dictionary<string, Dependency>  dependencies = new Dictionary<string, Dependency>();
        
        internal ReadDeps(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}

