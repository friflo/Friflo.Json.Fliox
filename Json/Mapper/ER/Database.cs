using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        public EntityContainer GetContainer<T>() where T : Entity
        {
            Type entityType = typeof(T);
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
        
        // protected internal abstract void     AddEntities   (Entity entity);
        // protected internal abstract Entity   GetEntity     (string id);
    }

    public abstract class EntityContainer<T> : EntityContainer where T : Entity
    {
        public override Type    EntityType => typeof(T);
        
        // convenience method
        public void Add(T entity) {
            var entities = new T[] {entity};
            AddEntities(entities);
        }
        
        // convenience method
        public T this[string id] {
            get {
                var ids = new string[] { id };
                var entities = GetEntities(ids);
                return entities.First();
            }
        }

        // ---
        public abstract void            AddEntities(IEnumerable<T> entities);
        public abstract IEnumerable<T>  GetEntities(IEnumerable<string> ids);
    }
    
    public class MemoryContainer<T> : EntityContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T>  map                 = new Dictionary<string, T>();

        public override int Count => map.Count;

        public override void AddEntities(IEnumerable<T> entities) {
            foreach (var entity in entities) {
                map.Add(entity.id, entity);
            }
        }

        public override IEnumerable<T> GetEntities(IEnumerable<string> ids) {
            var result = new List<T>();
            foreach (var id in ids) {
                result.Add(map[id]);
            }
            return result;
        }
    }
}