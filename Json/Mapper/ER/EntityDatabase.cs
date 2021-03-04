// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Friflo.Json.Mapper.ER
{
    public class EntityDatabase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, EntityContainer> containers = new Dictionary<Type, EntityContainer>();

        protected void AddContainer(EntityContainer cache) {
            Type entityType = cache.EntityType;
            containers.Add(entityType, cache);
        }

        public EntityContainer<T> GetContainer<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntityContainer container))
                return (EntityContainer<T>)container;
            containers[entityType] = container = new MemoryContainer<Entity>();
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
        public override Type    EntityType => typeof(T);
        
        // synchronous convenience method
        public void Add(T entity) {
            T[] entities = {entity};
            AddEntities(entities);
        }
        
        // synchronous convenience method
        public T this[string id] {
            get {
                string[] ids = { id };
                var entities = GetEntities(ids).Result;
                return entities.First();
            }
        }
        
        // ---
        public abstract Task                    AddEntities(IEnumerable<T> entities);
        public abstract Task<IEnumerable<T>>    GetEntities(IEnumerable<string> ids);
    }
    
    public class MemoryContainer<T> : EntityContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T>  map                 = new Dictionary<string, T>();

        public override int Count => map.Count;

#pragma warning disable 1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await TaskEx.Run(...)' to do CPU-bound work on a background thread
        public override async Task AddEntities(IEnumerable<T> entities) {
            foreach (var entity in entities) {
                map.Add(entity.id, entity);
            }
        }

        public override async Task<IEnumerable<T>> GetEntities(IEnumerable<string> ids) {
            var result = new List<T>();
            foreach (var id in ids) {
                result.Add(map[id]);
            }
            return result;
        }
#pragma warning restore 1998
    }
}