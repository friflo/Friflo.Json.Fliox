// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.value.json = patcher.Copy(entity.Value.value.json, true);
                }
            }
            return await entityContainer.CreateEntities(this);
        }
    }
    
    public partial class CreateEntitiesResult
    {
        internal override TaskType TaskType => TaskType.Create;
    }
    
    // ------ UpdateEntities
    public partial class UpdateEntities
    {
        internal override   TaskType    TaskType => TaskType.Update;
        public   override   string      ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.value.json = patcher.Copy(entity.Value.value.json, true);
                }
            }
            return await entityContainer.UpdateEntities(this);
        }
    }
    
    public partial class UpdateEntitiesResult
    {
        internal override TaskType TaskType => TaskType.Update;
    }
    
    // ------ ReadEntities
    public partial class ReadEntities
    {
        internal override   TaskType    TaskType => TaskType.Read;
        public   override   string      ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            var result = await entityContainer.ReadEntities(this);
            var entities = result.entities;
            result.entities = null; // clear -> its not part of protocol
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(entities);
            var readRefResults = await entityContainer.ReadReferences(references, entities, response);
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
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            var result = await entityContainer.QueryEntities(this);
            var containerResult = response.GetContainerResult(container);
            var entities = result.entities;
            result.entities = null;  // clear -> its not part of protocol
            containerResult.AddEntities(entities);
            var queryRefsResults = await entityContainer.ReadReferences(references, entities, response);
            result.container    = container;
            result.filterLinq   = filterLinq;
            result.ids          = entities.Keys.ToList();
            result.references   = queryRefsResults;
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
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            return await entityContainer.PatchEntities(this);
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
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            return await entityContainer.DeleteEntities(this);
        }
    }
    
    public partial class DeleteEntitiesResult
    {
        internal override TaskType      TaskType => TaskType.Delete;
    }
}
