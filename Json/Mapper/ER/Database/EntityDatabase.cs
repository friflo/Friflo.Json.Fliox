// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.ER.Database
{
    public class EntityDatabase : IDisposable
    {
        public readonly TypeStore   typeStore = new TypeStore();
        public readonly JsonMapper  mapper;
            
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, EntityContainer> containers = new Dictionary<Type, EntityContainer>();

        public EntityDatabase() {
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            mapper = new JsonMapper(typeStore);
        }
        
        public void Dispose() {
            mapper.Dispose();
            typeStore.Dispose();
        }

        internal void AddContainer<T>(EntityContainer<T> container) where T : Entity
        {
            containers.Add(typeof(T), container);
        }

        public EntityContainer<T> GetContainer<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntityContainer container))
                return (EntityContainer<T>)container;
            containers[entityType] = container = new MemoryContainer<Entity>(this);
            return (EntityContainer<T>)container;
        }
    }
    
    public abstract class EntityContainer
    {
        public abstract  Type       EntityType  { get; }
        public abstract  int        Count       { get; }
    }

    public abstract class EntityContainer<T> : EntityContainer where T : Entity
    {
        private     readonly    TypeMapper<T>   mapper;
        protected   readonly    EntityDatabase  database;
        public      override    Type            EntityType      => typeof(T);
        

        protected EntityContainer(EntityDatabase database) {
            database.AddContainer(this);
            this.database = database;
            mapper = (TypeMapper<T>)database.typeStore.GetTypeMapper(typeof(T));
        }
        
        // synchronous convenience method
        public void Create(T entity) {
            T[] entities = {entity};
            CreateEntities(entities);
        }
        
        // synchronous convenience method
        public void Update(T entity) {
            T[] entities = {entity};
            UpdateEntities(entities);
        }
        
        // synchronous convenience method
        public T Read(string id) {
            T entity = (T)mapper.CreateInstance();
            entity.id = id;
            T[] entities = { entity };
            var result = ReadEntities(entities).Result;
            return result.First();
        }
        
        // ---
        public abstract Task                    CreateEntities  (IEnumerable<T> entities);
        public abstract Task                    UpdateEntities  (IEnumerable<T> entities);
        public abstract Task<IEnumerable<T>>    ReadEntities    (IEnumerable<T> entities);
    }
}
