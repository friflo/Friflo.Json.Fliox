// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Friflo.Json.Mapper.ER.Database
{
    public class EntityDatabase : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();

        public EntityDatabase() {
        }
        
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
            containers[name] = container = new MemoryContainer(name, this);
            return container;
        }
    }
    
    public class KeyValue {
        public string key;
        public string value;
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
        
        // synchronous convenience method
        public void Create(KeyValue value) {
            KeyValue[] values = {value};
            CreateEntities(values);
        }
        
        // synchronous convenience method
        public void Update(KeyValue value) {
            KeyValue[] values = {value};
            UpdateEntities(values);
        }
        
        // synchronous convenience method
        public KeyValue Read(string id) {
            string[] ids = { id };
            var result = ReadEntities(ids).Result;
            return result.First();
        }
        
        // ---
        public abstract Task                            CreateEntities  (ICollection<KeyValue> entities);
        public abstract Task                            UpdateEntities  (ICollection<KeyValue> entities);
        public abstract Task<ICollection<KeyValue>>     ReadEntities    (ICollection<string> ids);
    }
}
