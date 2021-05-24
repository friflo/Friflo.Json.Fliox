// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class UpdateEntities : DatabaseTask
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
        
        internal override   TaskType            TaskType => TaskType.Update;
        public   override   string              ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = syncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.SetJson(patcher.Copy(entity.Value.Json, true));
                }
            }
            var result = await entityContainer.UpdateEntities(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            if (result.updateErrors != null && result.updateErrors.Count > 0) {
                var updateErrors = SyncResponse.GetEntityErrors(ref response.updateErrors, container);
                updateErrors.AddErrors(result.updateErrors);
            }
            return result;
        }
    }
    
    public class UpdateEntitiesResult : TaskResult, ICommandResult
    {
        public              CommandError                    Error { get; set; }
        [Fri.Ignore] public Dictionary<string, EntityError> updateErrors;

        internal override   TaskType            TaskType => TaskType.Update;
    }
}