// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class UpsertEntities : SyncRequestTask
    {
        [Fri.Required]  public  string          container;
                        public  string          keyName;
        [Fri.Required]  public  List<JsonValue> entities;
        
        [Fri.Ignore]    public  List<JsonKey>   entityKeys;
        
        internal override       TaskType        TaskType => TaskType.upsert;
        public   override       string          TaskName => $"container: '{container}'";
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            entityKeys = EntityUtils.GetKeysFromEntities(keyName, entities, executeContext, out string error);
            if (entityKeys == null) {
                return InvalidTask(error);
            }
            error = database.Schema?.ValidateEntities (container, entityKeys, entities, executeContext, EntityErrorType.WriteError, ref response.upsertErrors);
            if (error != null) {
                return TaskError(new CommandError(TaskErrorResultType.ValidationError, error));
            }
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooled = executeContext.pool.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooled.instance;
                    for (int n = 0; n < entities.Count; n++) {
                        var entity = entities[n];
                        // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                        // if (json == null)
                        //     return InvalidTask("value of entities key/value elements not be null");
                        entities[n] = patcher.Copy(entity, true);
                    }
                }
            }
            var result = await entityContainer.UpsertEntities(this, executeContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            if (result.upsertErrors != null && result.upsertErrors.Count > 0) {
                var upsertErrors = SyncResponse.GetEntityErrors(ref response.upsertErrors, container);
                upsertErrors.AddErrors(result.upsertErrors);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class UpsertEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Fri.Ignore] public CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    upsertErrors;

        internal override   TaskType                            TaskType => TaskType.upsert;
    }
}