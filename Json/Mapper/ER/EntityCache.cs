// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Friflo.Json.Mapper.ER
{
    public class EntityCache
    {
        private readonly EntityDatabase database;
        
        public EntityCache(EntityDatabase database) {
            this.database = database;
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, EntityCacheContainer> containers = new Dictionary<Type, EntityCacheContainer>();

        public async Task Sync() {
            foreach (var container in containers.Values) {
                await container.SyncContainer(database);
            }
        }

        protected void AddContainer(EntityCacheContainer cache) {
            Type entityType = cache.EntityType;
            containers.Add(entityType, cache);
        }

        public EntityCacheContainer<T> GetContainer<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntityCacheContainer container))
                return (EntityCacheContainer<T>)container;
            
            containers[entityType] = container = new MemoryCacheContainer<T>();
            return (EntityCacheContainer<T>)container;
        }
    }
    
    public abstract class EntityCacheContainer
    {
        public abstract  Type       EntityType  { get; }
        public abstract  int        Count       { get; }

        protected internal abstract Task SyncContainer   (EntityDatabase database);
    }

    public abstract class EntityCacheContainer<T> : EntityCacheContainer where T : Entity
    {
        public override Type    EntityType => typeof(T);
        
        // ---
        public abstract void    Add(T entity);
        public abstract T       this[string id] { get; } // Item[] Property
        
        protected internal abstract void    AddEntity   (T entity);
        protected internal abstract T       GetEntity   (string id);
    }
    
    public class MemoryCacheContainer<T> : EntityCacheContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T>  map                 = new Dictionary<string, T>();
        private readonly HashSet<string>        unresolvedEntities  = new HashSet<string>();

        public override int Count => map.Count;

        protected internal override void AddEntity   (T entity) {
            if (map.TryGetValue(entity.id, out T value)) {
                if (value != entity)
                    throw new InvalidOperationException("");
                return;
            }
            map.Add(entity.id, entity);
        }

        protected internal override T GetEntity(string id) {
            if (map.TryGetValue(id, out T entity))
                return entity;
            unresolvedEntities.Add(id);
            return null;
        }

        protected internal override async Task SyncContainer(EntityDatabase database) {
            EntityContainer<T> container = database.GetContainer<T>();
            var entities = await container.GetEntities(unresolvedEntities);
            foreach (var entity in entities) {
                map.Add(entity.id, entity);
            }
            unresolvedEntities.Clear();
        }
        
        // ---
        public override void Add(T entity) {
            map.Add(entity.id, entity);
        }
        
        public override T this[string id] => map[id];
    }
}