using System;
using System.Collections.Generic;

namespace Friflo.Json.Mapper.Map
{
    public class Database
    {
        private readonly Dictionary<Type, IDatabaseContainer> containers = new Dictionary<Type, IDatabaseContainer>();

        protected void AddContainer(IDatabaseContainer container) {
            Type entityType = container.EntityType;
            containers.Add(entityType, container);
        }

        public IDatabaseContainer GetContainer(Type entityType) {
            return containers[entityType];
        }
    }
    
    public interface IDatabaseContainer
    {
        Type    EntityType  { get; }
        int     Count       { get; }
        
        void    AddEntity   (Entity entity);
        void    RemoveEntity(string id);
        Entity  GetEntity   (string id);
    }

    public abstract class DatabaseContainer<T> : IDatabaseContainer where T : Entity
    {
        public Type         EntityType => typeof(T);
        public virtual int  Count =>  throw new NotImplementedException();

        public virtual void AddEntity(Entity entity) {
            throw new NotImplementedException();
        }

        public virtual void RemoveEntity(string id) {
            throw new NotImplementedException();
        }

        public virtual Entity GetEntity(string id) {
            throw new NotImplementedException();
        }
    }
    
    public class MemoryContainer<T> : DatabaseContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T> map = new Dictionary<string, T>();

        public override int Count => map.Count;

        public override void AddEntity   (Entity entity) {
            T typedEntity = (T) entity;
            map.Add(typedEntity.id, typedEntity);
        }

        public override void RemoveEntity(string id) {
            map.Remove(id);
        }

        public override Entity GetEntity(string id) {
            return map[id];
        }
    }
}