// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Database;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Sync
{
    // ----------------------------------- task -----------------------------------
    public class CreateEntities : DatabaseTask
    {
        [Fri.Required]  public  string                              container;
        [Fri.Required]  public  Dictionary<JsonKey, EntityValue>    entities = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
        
        internal override       TaskType                        TaskType => TaskType.create;
        public   override       string                          TaskName => $"container: '{container}'";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            
            database.schema?.ValidateEntities (container, entities, messageContext, EntityErrorType.WriteError, ref response.createErrors);

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
            var result = await entityContainer.CreateEntities(this, messageContext).ConfigureAwait(false);
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
    
    // ----------------------------------- task result -----------------------------------
    public class CreateEntitiesResult : TaskResult, ICommandResult
    {
                     public CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    createErrors;
        
        internal override   TaskType                        TaskType => TaskType.create;
    }
}