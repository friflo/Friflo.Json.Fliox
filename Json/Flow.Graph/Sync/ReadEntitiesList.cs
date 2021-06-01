// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    public class ReadEntitiesList : DatabaseTask
    {
        public  string                  container;
        public  List<ReadEntities>      reads;
        
        internal override   TaskType    TaskType => TaskType.read;
        public   override   string      ToString() => "container: " + container;

        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container == null)
                return MissingContainer();
            if (reads == null)
                return MissingField(nameof(reads));
            var result = new ReadEntitiesListResult {
                reads = new List<ReadEntitiesResult>(reads.Count)
            };
            // Optimization:
            // Combine all reads to a single read to call ReadEntities() only once instead of #reads times
            var combinedRead = new ReadEntities { ids = new HashSet<string>() };
            foreach (var read in reads) {
                if (read == null)
                    return InvalidTask("elements in reads must not be null");
                if (read.ids == null)
                    return MissingField(nameof(read.ids));
                foreach (var id in read.ids) {
                    if (id == null)
                        return InvalidTask("elements in ids must not be null");
                }
                if (!ValidReferences(read.references, out var error))
                    return error;
                combinedRead.ids.UnionWith(read.ids);
            }
            var entityContainer = database.GetOrCreateContainer(container);
            var combinedResult = await entityContainer.ReadEntities(combinedRead, syncContext).ConfigureAwait(false);
            if (combinedResult.Error != null) {
                return TaskError(combinedResult.Error);
            }
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
                var references = read.references;
                if (references != null && references.Count > 0) {
                    var readRefResults = await entityContainer.ReadReferences(references, readResult.entities, entityContainer.name, response, syncContext).ConfigureAwait(false);
                    if (readRefResults.error == null) {
                        readResult.references = readRefResults.references;
                    } else {
                        // tested with test "order-2" -> "read-task-error"
                        readResult.Error = readRefResults.error;
                    }
                }
                readResult.entities = null;
                result.reads.Add(readResult);
            }
            return result;
        }
    }
    
    public class ReadEntitiesListResult : TaskResult
    {
        public   List<ReadEntitiesResult>   reads;
        
        internal override   TaskType        TaskType => TaskType.read;
    }
}