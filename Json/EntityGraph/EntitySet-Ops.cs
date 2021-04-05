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
                throw new InvalidOperationException($"Read().Result requires Sync(). Entity: {typeof(T).Name} id: {id}");
            }
        }
        
        // lab - prototype API
        public Dependency<TValue> Dep<TValue>(string selector) where TValue : Entity {
            if (!set.readDeps.TryGetValue(selector, out ReadDeps readDeps)) {
                readDeps = new ReadDeps(selector, typeof(TValue));
                set.readDeps.Add(selector, readDeps);
            }
            Dependency<TValue> newDependency = new Dependency<TValue>(id);
            readDeps.dependencies.Add(newDependency);
            return newDependency;
        }

        // lab - expression API
        public Dependency<TValue> Dependency<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            return default;
        }
        
        // lab - expression API
        public IEnumerable<Dependency<TValue>> Dependencies<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity 
        {
            return default;
        }

        // lab - dependencies by Entity Type
        public IEnumerable<Dependency<TValue>> DependenciesOfType<TValue>() where TValue : Entity
        {
            return default;
        }
        
        // lab - all dependencies
        public IEnumerable<Dependency<Entity>> AllDependencies()
        {
            return default;
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
        internal            string      id;

        public              string      Id => id ?? throw new InvalidOperationException("Dependency not synced"); 

        internal Dependency(string parentId) {
            this.parentId = parentId;
        }
    }
    
    public class Dependency<T> : Dependency where T : Entity
    {
        internal            T           entity;
        
        public              T           Entity => entity ?? throw new InvalidOperationException("Dependency not synced"); 

        internal Dependency(string parentId) : base (parentId) { }
    }
    
    internal class ReadDeps
    {
        internal readonly   string              selector;
        internal readonly   Type                entityType;
        internal readonly   List<Dependency>    dependencies = new List<Dependency>();
        
        internal ReadDeps(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}

