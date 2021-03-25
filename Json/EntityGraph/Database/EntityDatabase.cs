// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph.Database
{
    
    public abstract class EntityDatabase : IDisposable
    {
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();

        protected abstract EntityContainer CreateContainer(string name, EntityDatabase database);
        
        public void Dispose() {
        }

        internal void AddContainer(EntityContainer container)
        {
            containers.Add(container.name, container);
        }

        public EntityContainer GetContainer(string name)
        {
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
    }
    
    public class KeyValue {
        public string       key;
        public JsonValue    value;
    }
    
    public abstract class EntityContainer
    {
        public      readonly    string          name;
        protected   readonly    EntityDatabase  database;

        protected EntityContainer(string name, EntityDatabase database) {
            this.name = name;
            database.AddContainer(this);
            this.database = database;
        }
        
        // ---
        public abstract void                      CreateEntities  (ICollection<KeyValue> entities);
        public abstract void                      UpdateEntities  (ICollection<KeyValue> entities);
        public abstract ICollection<KeyValue>     ReadEntities    (ICollection<string> ids);
    }
}
