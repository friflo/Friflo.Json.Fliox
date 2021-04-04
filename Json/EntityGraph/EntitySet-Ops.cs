// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- CRUD -----------------------------------------
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
        public Read<TValue> Dep<TValue>(string selector) where TValue : Entity {
            if (!set.readDeps.TryGetValue(selector, out ReadDeps depsById)) {
                depsById = new ReadDeps(selector, typeof(TValue));
                set.readDeps.Add(selector, depsById);
            }
            var dependency = new Dependency<TValue>(id);
            depsById.dependencies.Add(dependency);
            return default;
        }

        // lab - expression API
        public Read<TValue> Dependency<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            return default;
        }
        
        // lab - expression API
        public IEnumerable<Read<TValue>> Dependencies<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity 
        {
            return default;
        }

        // lab - dependencies by Entity Type
        public IEnumerable<Read<TValue>> DependenciesOfType<TValue>() where TValue : Entity
        {
            return default;
        }
        
        // lab - all dependencies
        public IEnumerable<Read<Entity>> AllDependencies()
        {
            return default;
        }

    }
    
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

    public interface IDependency
    {
        string Id { get; }
    }
    
    public class Dependency<T> : IDependency where T : Entity
    {
        private readonly    string          id;
        // private readonly    EntitySet<T>    set;
        public              string          Id => id;
        
        internal Dependency(string id) {
            this.id = id;
            // this.set = set;
        }

    }
    
    public class ReadDeps
    {
        internal readonly   string              selector;
        internal readonly   Type                entityType;
        internal readonly   List<IDependency>   dependencies = new List<IDependency>();
        
        internal ReadDeps(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }

    // ------------------------------------- PeerEntity<> -------------------------------------
    internal class PeerEntity<T>  where T : Entity
    {
        internal readonly   T           entity;
        internal            T           patchReference; 
        internal            T           nextPatchReference; 
        internal            bool        assigned;
        internal            Read<T>     read;
        internal            Create<T>   create;

        internal PeerEntity(T entity) {
            this.entity = entity;
        }
    }

    public class PeerNotAssignedException : Exception
    {
        public readonly Entity entity;
        
        public PeerNotAssignedException(Entity entity) : base ($"Entity: {entity.GetType().Name} id: {entity.id}") {
            this.entity = entity;
        }
    }
}
