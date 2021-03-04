using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Friflo.Json.Mapper.ER
{
    public class Database
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, EntityContainer> containers = new Dictionary<Type, EntityContainer>();

        protected void AddContainer(EntityContainer cache) {
            Type entityType = cache.EntityType;
            containers.Add(entityType, cache);
        }

        public EntityContainer GetContainer(Type entityType) {
            if (containers.TryGetValue(entityType, out EntityContainer container))
                return container;
            containers[entityType] = container = new MemoryContainer<Entity>();
            return container;
        }
    }
    
    public abstract class EntityContainer
    {
        public abstract  Type       EntityType  { get; }
        public abstract  int        Count       { get; }
        
        protected internal abstract void     AddEntity   (Entity entity);
        protected internal abstract Entity   GetEntity   (string id);
    }

    public abstract class EntityContainer<T> : EntityContainer where T : Entity
    {
        public override Type    EntityType => typeof(T);
        
        // ---
        public abstract void    Add(T entity);
        public abstract T       this[string id] { get; } // Item[] Property
    }
    
    public class MemoryContainer<T> : EntityContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T>  map                 = new Dictionary<string, T>();
        private readonly HashSet<string>        unresolvedEntities  = new HashSet<string>();

        public override int Count => map.Count;

        protected internal override void AddEntity   (Entity entity) {
            T typedEntity = (T) entity;
            if (map.TryGetValue(entity.id, out T value)) {
                if (value != entity)
                    throw new InvalidOperationException("");
                return;
            }
            map.Add(typedEntity.id, typedEntity);
        }

        protected internal override Entity GetEntity(string id) {
            if (map.TryGetValue(id, out T entity))
                return entity;
            unresolvedEntities.Add(id);
            return null;
        }
        
        // ---
        public override void Add(T entity) {
            map.Add(entity.id, entity);
        }
        
        public override T this[string id] => map[id];
    }
}