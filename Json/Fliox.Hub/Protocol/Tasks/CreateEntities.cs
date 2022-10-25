// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Create the given <see cref="entities"/> in the specified <see cref="container"/>
    /// </summary>
    public sealed class CreateEntities : SyncRequestTask
    {
        /// <summary>container name the <see cref="entities"/> are created</summary>
        [Required]  public  string              container;
                    public  Guid?               reservedToken;
        /// <summary>name of the primary key property in <see cref="entities"/></summary>
                    public  string              keyName;
        /// <summary>the <see cref="entities"/> which are created in the specified <see cref="container"/></summary>
        [Required]  public  List<JsonEntity>    entities;
                        
        public   override   TaskType            TaskType => TaskType.create;
        public   override   string              TaskName => $"container: '{container}'";
        
        public override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            if (!EntityUtils.GetKeysFromEntities(keyName, entities, syncContext, out string error)) {
                return InvalidTask(error);
            }

            List<EntityError> validationErrors = null;
            error = database.Schema?.ValidateEntities (container, entities, syncContext, EntityErrorType.WriteError, ref validationErrors);
            if (error != null) {
                return TaskError(new CommandError(TaskErrorResultType.ValidationError, error));
            }
            
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooled = syncContext.pool.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooled.instance;
                    for (int n = 0; n < entities.Count; n++) {
                        var entity = entities[n];
                        // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                        entities[n] = new JsonEntity(entity.key, patcher.Copy(entity.value, true));
                    }
                }
            }
            var result = await entityContainer.CreateEntities(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            SyncResponse.AddEntityErrors(ref result.errors, validationErrors);
            return result;
        }
    }

    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="CreateEntities"/> task
    /// </summary>
    public sealed class CreateEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to create</summary>
                    public List<EntityError>   errors;
        
        internal override  TaskType            TaskType => TaskType.create;
    }
}