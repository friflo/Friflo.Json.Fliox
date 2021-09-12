// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- task -----------------------------------
    public class ReadEntitiesList : DatabaseTask
    {
        [Fri.Required]  public  string              container;
        [Fri.Required]  public  List<ReadEntities>  reads;
        
        internal override       TaskType            TaskType => TaskType.read;
        public   override       string              TaskName =>  $"container: '{container}'";

        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (reads == null)
                return MissingField(nameof(reads));
            var result = new ReadEntitiesListResult {
                reads = new List<ReadEntitiesResult>(reads.Count)
            };
            // Optimization:
            // Count & Combine all reads to a single read to call ReadEntities() only once instead of #reads times
            var combineCount = 0;
            foreach (var read in reads) {
                if (read == null)
                    return InvalidTask("elements in reads must not be null");
                if (read.ids == null)
                    return MissingField(nameof(read.ids));
                foreach (var id in read.ids) {
                    if (id.IsNull())
                        return InvalidTask("elements in ids must not be null");
                }
                if (!ValidReferences(read.references, out var error))
                    return error;
                combineCount += read.ids.Count;
            }
            // Combine
            var combinedRead = new ReadEntities { ids = Helper.CreateHashSet(combineCount, JsonKey.Equality) };
            foreach (var read in reads) {
                combinedRead.ids.UnionWith(read.ids);
            }
            var entityContainer = database.GetOrCreateContainer(container);
            var combinedResult = await entityContainer.ReadEntities(combinedRead, messageContext).ConfigureAwait(false);
            if (combinedResult.Error != null) {
                return TaskError(combinedResult.Error);
            }
            var combinedEntities = combinedResult.entities;
            combinedResult.entities = null;
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(combinedEntities);
            
            foreach (var read in reads) {
                var readResult  = new ReadEntitiesResult {
                    entities = new Dictionary<JsonKey, EntityValue>(read.ids.Count, JsonKey.Equality)
                };
                // distribute combinedEntities
                var entities = readResult.entities;
                foreach (var id in read.ids) {
                    entities.Add(id, combinedEntities[id]);
                }
                var references = read.references;
                if (references != null && references.Count > 0) {
                    var readRefResults =
                        await entityContainer.ReadReferences(references, entities, entityContainer.name, "", response, messageContext).ConfigureAwait(false);
                    // returned readRefResults.references is always set. Each references[] item contain either a result or an error.
                    readResult.references = readRefResults.references;
                }
                readResult.entities = null;
                result.reads.Add(readResult);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class ReadEntitiesListResult : TaskResult
    {
        [Fri.Required]  public  List<ReadEntitiesResult>    reads;
        
        internal override       TaskType                    TaskType => TaskType.read;
    }
}