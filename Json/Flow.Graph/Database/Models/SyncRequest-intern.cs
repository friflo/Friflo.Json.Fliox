// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Burst; // UnityExtension.TryAdd()

namespace Friflo.Json.Flow.Database.Models
{
    // ------------------------------ SyncRequest / SyncResponse ------------------------------
    public partial class SyncResponse
    {
        internal ContainerEntities GetContainerResult(string container) {
            if (results.TryGetValue(container, out ContainerEntities result))
                return result;
            result = new ContainerEntities {
                container = container,
                entities = new Dictionary<string,EntityValue>()
            };
            results.Add(container, result);
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
    
    // ------ ReadEntitiesList & ReadEntities
    public partial class ReadEntitiesList
    {
        internal override   TaskType    TaskType => TaskType.Read;
        public   override   string      ToString() => "container: " + container;

        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var result = new ReadEntitiesListResult {
                reads = new List<ReadEntitiesResult>(reads.Count)
            };
            // Optimization:
            // Combine all reads to a single read to call ReadEntities() only once instead of #reads times
            var combinedRead = new ReadEntities { ids = new HashSet<string>() };
            foreach (var read in reads) {
                combinedRead.ids.UnionWith(read.ids);
            }
            var entityContainer = database.GetContainer(container);
            var combinedResult = await entityContainer.ReadEntities(combinedRead);
            
            var combinedEntities = combinedResult.entities;
            combinedResult.entities = null;
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(combinedEntities);
            
            foreach (var read in reads) {
                var readResult  = new ReadEntitiesResult {
                    entities = new Dictionary<string, EntityValue>(read.ids.Count)
                };
                // distribute combinedEntities
                foreach (var id in read.ids) {
                    readResult.entities.Add(id, combinedEntities[id]);
                }
                await read.ReadReferences(readResult, entityContainer, response);
                readResult.entities = null;
                result.reads.Add(readResult);
            }
            return result;
        }
    }
    
    public partial class ReadEntitiesListResult
    {
        internal override   TaskType    TaskType => TaskType.Read;
    }
    
    public partial class ReadEntities
    {
        public   override   string      ToString() => "container: " + container;

        internal async Task ReadReferences(ReadEntitiesResult readResult, EntityContainer entityContainer, SyncResponse response) {
            List<ReferencesResult> readRefResults = null;
            if (references != null && references.Count > 0) {
                readRefResults = await entityContainer.ReadReferences(references, readResult.entities, response);
            }
            readResult.references = readRefResults;
        }
    }
    
    public partial class ReadEntitiesResult
    {
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
            List<ReferencesResult> queryRefsResults = null;
            if (references != null && references.Count > 0) {
                queryRefsResults = await entityContainer.ReadReferences(references, entities, response);
            }
            result.container    = container;
            result.filterLinq   = filterLinq;
            result.ids          = entities.Keys.ToHashSet();
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
