// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- task -----------------------------------
    public sealed class CreateEntities : SyncRequestTask
    {
        [Fri.Required]  public  string          container;
                        public  Guid?           reservedToken;
                        public  string          keyName;
        [Fri.Required]  public  List<JsonValue> entities;
                        
        [Fri.Ignore]    public  List<JsonKey>   entityKeys;
        
        internal override       TaskType        TaskType => TaskType.create;
        public   override       string          TaskName => $"container: '{container}'";
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            entityKeys = EntityUtils.GetKeysFromEntities(keyName, entities, messageContext, out string error);
            if (entityKeys == null) {
                return InvalidTask(error);
            }

            error = database.schema?.ValidateEntities (container, entityKeys, entities, messageContext, EntityErrorType.WriteError, ref response.createErrors);
            if (error != null) {
                return TaskError(new CommandError(error));
            }
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooledPatcher = messageContext.pools.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooledPatcher.instance;
                    for (int n = 0; n < entities.Count; n++) {
                        var entity = entities[n];
                        // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                        entities[n] = new JsonValue(patcher.Copy(entity.json, true));
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
    public sealed class CreateEntitiesResult : SyncTaskResult, ICommandResult
    {
                     public CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    createErrors;
        
        internal override   TaskType                            TaskType => TaskType.create;
    }
}