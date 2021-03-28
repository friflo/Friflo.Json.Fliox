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
        private readonly Dictionary<string, EntityContainer>    containers = new Dictionary<string, EntityContainer>();
        
        public abstract EntityContainer CreateContainer(string name, EntityDatabase database);

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
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
        
        public virtual SyncResponse Execute(SyncRequest syncRequest) {
            var response = new SyncResponse { results = new List<CommandResult>() };
            foreach (var command in syncRequest.commands) {
                var result = command.Execute(this);
                response.results.Add(result);
            }
            return response;
        }
    }
    
    public class KeyValue {
        public string       key;
        public JsonValue    value;

        public override string ToString() => key ?? "null";
    }
    
    public abstract class EntityContainer : IDisposable
    {
        public  readonly    string          name;

        public virtual      bool            Pretty => false;
        public virtual      CommandContext  CommandContext => null;


        protected EntityContainer(string name, EntityDatabase database) {
            this.name = name;
            database.AddContainer(this);
            // this.database = database;
        }
        
        public virtual void Dispose() { }
        
        // ---
        public abstract void                      CreateEntities  (ICollection<KeyValue> entities);
        public abstract void                      UpdateEntities  (ICollection<KeyValue> entities);
        public abstract ICollection<KeyValue>     ReadEntities    (ICollection<string> ids);

    }
}
