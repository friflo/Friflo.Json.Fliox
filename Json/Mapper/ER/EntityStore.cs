// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Friflo.Json.Mapper.ER
{
    public class EntityStore
    {
        private readonly EntityDatabase database;
        public  readonly TypeStore      typeStore;
        
        public EntityStore(EntityDatabase database) {
            this.database = database;
            typeStore = database.typeStore;
        }
        
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, EntityCacheContainer> containers = new Dictionary<Type, EntityCacheContainer>();

        public async Task Sync() {
            foreach (var container in containers.Values) {
                await container.SyncContainer(database);
            }
        }

        public EntityCacheContainer<T> GetContainer<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntityCacheContainer container))
                return (EntityCacheContainer<T>)container;
            
            containers[entityType] = container = new EntityCacheContainer<T>();
            return (EntityCacheContainer<T>)container;
        }
    }
    
    public abstract class EntityCacheContainer
    {
        protected internal abstract Task SyncContainer   (EntityDatabase database);
    }

   
    public class EntityCacheContainer<T> : EntityCacheContainer where T : Entity
    {
        private readonly    Dictionary<string, T>   map                 = new Dictionary<string, T>();
        private readonly    HashSet<string>         unresolvedEntities  = new HashSet<string>();

        public              int                     Count       => map.Count;

        protected internal void AddEntity   (T entity) {
            if (map.TryGetValue(entity.id, out T value)) {
                if (value != entity)
                    throw new InvalidOperationException("");
                return;
            }
            map.Add(entity.id, entity);
        }

        public T this[string id] => map[id];
        
        protected internal T GetEntity(string id) {
            if (map.TryGetValue(id, out T entity))
                return entity;
            unresolvedEntities.Add(id);
            return null;
        }

        protected internal override async Task SyncContainer(EntityDatabase database) {
            if (unresolvedEntities.Count == 0)
                return;
            
            EntityContainer<T> container = database.GetContainer<T>();
            var entities = await container.GetEntities(unresolvedEntities);
            foreach (var entity in entities) {
                map.Add(entity.id, entity);
            }
            unresolvedEntities.Clear();
        }
        
    }
}