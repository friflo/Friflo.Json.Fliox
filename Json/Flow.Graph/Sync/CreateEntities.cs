// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class CreateEntities : DatabaseTask
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
        
        internal override   TaskType            TaskType => TaskType.Create;
        public   override   string              ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.SetJson(patcher.Copy(entity.Value.Json, true));
                }
            }
            var result = await entityContainer.CreateEntities(this);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            if (result.createErrors != null && result.createErrors.Count > 0) {
                var createErrors = SyncResponse.GetEntityErrors(ref response.createErrors, container);
                createErrors.AddErrors(result.createErrors);
            }
            return result;
        }
    }
    
    public class CreateEntitiesResult : TaskResult, ICommandResult
    {
                     public CommandError                    Error { get; set; }
        [Fri.Ignore] public Dictionary<string, EntityError> createErrors;
        
        internal override TaskType              TaskType => TaskType.Create;
    }
}