// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class DeleteEntities : DatabaseTask
    {
        public              string              container;
        public              HashSet<string>     ids;
        
        internal override   TaskType            TaskType => TaskType.Delete;
        public   override   string              ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.DeleteEntities(this);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            if (result.deleteErrors != null && result.deleteErrors.Count > 0) {
                var deleteErrors = SyncResponse.GetEntityErrors(ref response.deleteErrors, container);
                deleteErrors.AddErrors(result.deleteErrors);
            }
            return result;
        }
    }
    
    public class DeleteEntitiesResult : TaskResult, ICommandResult
    {
                     public CommandError                    Error { get; set; }
        [Fri.Ignore] public Dictionary<string, EntityError> deleteErrors;

        internal override   TaskType            TaskType => TaskType.Delete;
    }
}