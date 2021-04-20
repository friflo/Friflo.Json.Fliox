// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
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
        internal override   TaskType    TaskType => TaskType.Create;
        public   override   string      ToString() => "container: " + container;
        
        internal override TaskResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.value.json = patcher.Copy(entity.Value.value.json, true);
                }
            }
            return entityContainer.CreateEntities(this);
        }
    }
    
    public partial class CreateEntitiesResult
    {
        internal override TaskType TaskType => TaskType.Create;
    }
    
    // ------ ReadEntities
    public partial class ReadEntities
    {
        internal override   TaskType    TaskType => TaskType.Read;
        public   override   string      ToString() => "container: " + container;
        
        internal override TaskResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            var result = entityContainer.ReadEntities(this);
            var entities = result.entities;
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(entities);
            var readRefResults = entityContainer.ReadReferences(references, entities, response);
            result.references = readRefResults;
            return result;
        }
    }
    
    public partial class ReadEntitiesResult
    {
        internal override   TaskType    TaskType => TaskType.Read;
    }
    
    // ------ QueryEntities
    public partial class QueryEntities 
    {
        internal override   TaskType    TaskType => TaskType.Query;
        public   override   string      ToString() => $"container: {container}, filter: {filterLinq}";
        
        internal override TaskResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            var entities = entityContainer.QueryEntities(filter);
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(entities);
            
            var result = new QueryEntitiesResult {
                container   = container,
                filterLinq  = filterLinq,
                ids         = entities.Keys.ToList()
            };
            return result;
        }
    }
    
    public partial class QueryEntitiesResult
    {
        internal override   TaskType    TaskType => TaskType.Query;
        public   override   string      ToString() => $"container: {container}, filter: {filterLinq}";
    }
    
    // ------ PatchEntities
    public partial class PatchEntities
    {
        internal override   TaskType    TaskType => TaskType.Patch;
        public   override   string      ToString() => "container: " + container;
        
        internal override TaskResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            entityContainer.PatchEntities(entityPatches);
            return new PatchEntitiesResult(); 
        }
    }
    
    public partial class PatchEntitiesResult
    {
        internal override TaskType      TaskType => TaskType.Patch;
    }
    
    // ------ DeleteEntities
    public partial class DeleteEntities
    {
        internal override   TaskType    TaskType => TaskType.Delete;
        public   override   string      ToString() => "container: " + container;
        
        internal override TaskResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            return entityContainer.DeleteEntities(this);
        }
    }
    
    public partial class DeleteEntitiesResult
    {
        internal override TaskType      TaskType => TaskType.Delete;
    }
}
