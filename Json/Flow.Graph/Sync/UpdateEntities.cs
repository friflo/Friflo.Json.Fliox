// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class UpdateEntities : DatabaseTask
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
        
        internal override   TaskType            TaskType => TaskType.update;
        public   override   string              TaskName => $"container: '{container}'";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooledPatcher = messageContext.pools.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooledPatcher.instance;
                    foreach (var entity in entities) {
                        var value = entity.Value;
                        if (value.Json == null)
                            return InvalidTask("value of entities key/value elements not be null");
                        value.SetJson(patcher.Copy(value.Json, true));
                    }
                }
            }
            var result = await entityContainer.UpdateEntities(this, messageContext).ConfigureAwait(false);
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
    
    // ----------------------------------- task result -----------------------------------
    public class UpdateEntitiesResult : TaskResult, ICommandResult
    {
        public              CommandError                    Error { get; set; }
        [Fri.Ignore] public Dictionary<string, EntityError> updateErrors;

        internal override   TaskType                        TaskType => TaskType.update;
    }
}