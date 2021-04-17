// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst; // UnityExtension.TryAdd()

namespace Friflo.Json.EntityGraph.Database
{
    // ------------------------------ SyncRequest / SyncResponse ------------------------------
    public partial class SyncResponse
    {
        internal ContainerEntities GetContainerResult(string container) {
            if (containerResults.TryGetValue(container, out ContainerEntities result))
                return result;
            result = new ContainerEntities {
                container = container,
                entities = new Dictionary<string,EntityValue>()
            };
            containerResults.Add(container, result);
            return result;
        }
    }
    
    // ------ ContainerEntities
    public partial class ContainerEntities
    {
        internal void AddEntities(Dictionary<string, EntityValue> add) {
            foreach (var entity in add) {
                entities.TryAdd(entity.Key, entity.Value);
            }
        }
    }
    
    // ------ CreateEntities
    public partial class CreateEntities
    {
        internal override CommandType   CommandType => CommandType.Create;
        public   override string        ToString() => "container: " + container;
        
        internal override CommandResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.value.json = patcher.Copy(entity.Value.value.json, true);
                }
            }
            entityContainer.CreateEntities(entities);
            return new CreateEntitiesResult();
        }
    }
    
    // ------ ReadEntities
    public partial class ReadEntities
    {
        internal override CommandType   CommandType => CommandType.Read;
        public   override string        ToString() => "container: " + container;
        
        internal override CommandResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            var entities = entityContainer.ReadEntities(ids);
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(entities);
            var readRefResults = entityContainer.ReadReferences(references, entities, response);
            var result = new ReadEntitiesResult {
                references = readRefResults
            };
            return result;
        }
    }
    
    public partial class ReadEntitiesResult
    {
        internal override CommandType CommandType => CommandType.Read;
    }
    
    // ------ PatchEntities
    public partial class PatchEntities
    {
        internal override CommandType   CommandType => CommandType.Patch;
        public   override string        ToString() => "container: " + container;
        
        internal override CommandResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            entityContainer.PatchEntities(entityPatches);
            return new PatchEntitiesResult(); 
        }
    }
}
