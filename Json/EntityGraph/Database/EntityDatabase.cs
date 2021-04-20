// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.EntityGraph.Database
{
    // ----------------------------------------- EntityDatabase -----------------------------------------
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
            var response = new SyncResponse {
                results             = new List<TaskResult>(),
                containerResults    = new Dictionary<string, ContainerEntities>()
            };
            foreach (var task in syncRequest.tasks) {
                var result = task.Execute(this, response);
                response.results.Add(result);
            }
            return response;
        }
    }
}
