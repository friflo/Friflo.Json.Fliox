// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Sync
{
    public class QueryEntities : DatabaseTask
    {
        public  string                      container;
        public  string                      filterLinq;
        public  FilterOperation             filter;
        public  List<References>            references;
        
        internal override   TaskType        TaskType => TaskType.Query;
        public   override   string          ToString() => $"container: {container}, filter: ({filterLinq})";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.QueryEntities(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            var containerResult = response.GetContainerResult(container);
            var entities = result.entities;
            result.entities = null;  // clear -> its not part of protocol
            containerResult.AddEntities(entities);
            var queryRefsResults = new ReadReferencesResult();
            if (references != null && references.Count > 0) {
                queryRefsResults = await entityContainer.ReadReferences(references, entities, container, response, syncContext).ConfigureAwait(false);
                if (queryRefsResults.error != null) {
                    return TaskError(queryRefsResults.error); // todo add error test
                }
            }
            result.container    = container;
            result.filterLinq   = filterLinq;
            result.ids          = entities.Keys.ToHashSet();
            result.references   = queryRefsResults.references;
            return result;
        }
    }
    
    public class QueryEntitiesResult : TaskResult, ICommandResult
    {
        public  string                          container;  // only for debugging ergonomics
        public  string                          filterLinq;
        public  HashSet<string>                 ids;
        public  List<ReferencesResult>          references;
        [Fri.Ignore]
        public  Dictionary<string,EntityValue>  entities;
        public  CommandError                    Error { get; set; }

        
        internal override   TaskType            TaskType => TaskType.Query;
        public   override   string              ToString() => $"container: {container}, filter: ({filterLinq})";
    }
}