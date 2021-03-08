// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Mapper.ER.Database;
using Friflo.Json.Mapper.Map;

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
        internal readonly Dictionary<Type, EntityStoreContainer> containers = new Dictionary<Type, EntityStoreContainer>();

        public async Task Sync() {
            foreach (var container in containers.Values) {
                await container.SyncContainer(database);
            }
        }

        public EntityStoreContainer<T> GetContainer<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntityStoreContainer container))
                return (EntityStoreContainer<T>)container;
            
            container = new EntityStoreContainer<T>(this);
            return (EntityStoreContainer<T>)container;
        }
    }
    
    public abstract class EntityStoreContainer
    {
        protected internal abstract Task SyncContainer   (EntityDatabase database);
    }

   
    public class EntityStoreContainer<T> : EntityStoreContainer where T : Entity
    {
        private readonly    TypeMapper<T>           mapper;
        private readonly    Dictionary<string, T>   map                 = new Dictionary<string, T>();  // todo -> HashSet<>
        private readonly    List<T>                 unresolvedEntities  = new List<T>();  // todo -> HashSet<>

        public              int                     Count       => map.Count;
        
        public EntityStoreContainer(EntityStore store) {
            store.containers[typeof(T)] = this;
            mapper = (TypeMapper<T>)store.typeStore.GetTypeMapper(typeof(T));
        }
        
        protected internal void Create   (T entity) {
            if (map.TryGetValue(entity.id, out T value)) {
                if (value != entity)
                    throw new InvalidOperationException("");
                return;
            }
            map.Add(entity.id, entity);
        }

        public T Read(string id) {
            if (map.TryGetValue(id, out T entity))
                return entity;
            entity = (T)mapper.CreateInstance();
            entity.id = id;
            unresolvedEntities.Add(entity);
            map.Add(id, entity);
            return entity;
        }

        protected internal override async Task SyncContainer(EntityDatabase database) {
            if (unresolvedEntities.Count == 0)
                return;
            
            EntityContainer<T> container = database.GetContainer<T>();
            var entities = await container.ReadEntities(unresolvedEntities);
            /* foreach (var entity in entities) {
                map.Add(entity.id, entity);
            } */
            unresolvedEntities.Clear();
        }
        
    }
}