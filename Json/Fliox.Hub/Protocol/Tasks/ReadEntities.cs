// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Read entities by id from the specified <see cref="container"/> using read <see cref="sets"/><br/>
    /// Each <see cref="ReadEntitiesSet"/> contains a list of <see cref="ReadEntitiesSet.ids"/><br/>
    /// To return also entities referenced by entities listed in <see cref="ReadEntitiesSet.ids"/> use
    /// <see cref="ReadEntitiesSet.references"/> in <see cref="sets"/>. <br/>
    /// This mimic the functionality of a <b>JOIN</b> in <b>SQL</b>
    /// </summary>
    public sealed class ReadEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Fri.Required]  public  string                  container;
        /// <summary>name of the primary key property of the returned entities</summary>
                        public  string                  keyName;
                        public  bool?                   isIntKey;
        /// <summary>contains the <see cref="ReadEntitiesSet.ids"/> of requested entities</summary>               
        [Fri.Required]  public  List<ReadEntitiesSet>   sets;
        
        internal override       TaskType                TaskType => TaskType.read;
        public   override       string                  TaskName =>  $"container: '{container}'";

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (container == null)
                return MissingContainer();
            if (sets == null)
                return MissingField(nameof(sets));
            var result = new ReadEntitiesResult {
                sets = new List<ReadEntitiesSetResult>(sets.Count)
            };
            // Optimization:
            // Count & Combine all reads to a single read to call ReadEntitiesSet() only once instead of #reads times
            var combineCount = 0;
            foreach (var read in sets) {
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
            var combinedRead = new ReadEntitiesSet { keyName = keyName, isIntKey = isIntKey, ids = Helper.CreateHashSet(combineCount, JsonKey.Equality) };
            foreach (var read in sets) {
                combinedRead.ids.UnionWith(read.ids);
            }
            var entityContainer = database.GetOrCreateContainer(container);
            var combinedResult = await entityContainer.ReadEntitiesSet(combinedRead, executeContext).ConfigureAwait(false);
            if (combinedResult.Error != null) {
                return TaskError(combinedResult.Error);
            }
            var combinedEntities = combinedResult.entities;
            combinedResult.entities = null;
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(combinedEntities);
            
            foreach (var read in sets) {
                var readResult  = new ReadEntitiesSetResult {
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
                        await entityContainer.ReadReferences(references, entities, entityContainer.name, "", response, executeContext).ConfigureAwait(false);
                    // returned readRefResults.references is always set. Each references[] item contain either a result or an error.
                    readResult.references = readRefResults.references;
                }
                readResult.entities = null;
                result.sets.Add(readResult);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class ReadEntitiesResult : SyncTaskResult
    {
        [Fri.Required]  public  List<ReadEntitiesSetResult> sets;
        
        internal override       TaskType                    TaskType => TaskType.read;
    }
}