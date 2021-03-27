// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- CRUD -----------------------------------------
    public class Read<T>
    {
        private  readonly   string id;
        internal            T      result;
        internal            bool   synced;

        internal Read(string id) {
            this.id = id;
        }
            
        public T Result {
            get {
                if (synced)
                    return result;
                throw new InvalidOperationException($"Read().Result requires Sync(). Entity: {typeof(T).Name} id: {id}");
            }
        }
    }
    
    public class Create<T>
    {
        private readonly    T           entity;
        private readonly    EntityStore store;

        internal            T           Entity => entity;
        
        internal Create(T entity, EntityStore entityStore) {
            this.entity = entity;
            this.store = entityStore;
        }

        public Create<T> Dependencies() {
            var tracer = new Tracer(store.intern.jsonMapper.writer.TypeCache, store);
            tracer.Trace(entity);
            return this;
        }
            
        // public T Result  => entity;
    }

    // ------------------------------------- PeerEntity<> -------------------------------------
    internal class PeerEntity<T>  where T : Entity
    {
        internal readonly   T           entity;
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
