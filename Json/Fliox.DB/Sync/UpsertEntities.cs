// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- task -----------------------------------
    public class UpsertEntities : DatabaseTask
    {
        [Fri.Required]  public  string                          container;
        [Fri.Required]  public  string                          keyName;
        [Fri.Required]  public  List<EntityValue>               entities;
        
        [Fri.Ignore]    public  List<JsonKey>                   entityKeys;
        
        internal override       TaskType                        TaskType => TaskType.upsert;
        public   override       string                          TaskName => $"container: '{container}'";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            if (keyName == null)
                return MissingField(nameof(keyName));
            entityKeys = EntityContainer.CreateEntityKeys(keyName, entities, messageContext, out string error);
            if (entityKeys == null) {
                return InvalidTask(error);
            }
            database.schema?.ValidateEntities (container, entityKeys, entities, messageContext, EntityErrorType.WriteError, ref response.updateErrors);
            
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooledPatcher = messageContext.pools.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooledPatcher.instance;
                    foreach (var entity in entities) {
                        if (entity == null) // TAG_ENTITY_NULL
                            continue;
                        var json = entity.Json;
                        if (json == null)
                            return InvalidTask("value of entities key/value elements not be null");
                        entity.SetJson(patcher.Copy(json, true));
                    }
                }
            }
            var result = await entityContainer.UpsertEntities(this, messageContext).ConfigureAwait(false);
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
    public class UpsertEntitiesResult : TaskResult, ICommandResult
    {
        public              CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    updateErrors;

        internal override   TaskType                        TaskType => TaskType.upsert;
    }
}