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
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Create the given <see cref="entities"/> in the specified <see cref="container"/>
    /// </summary>
    public sealed class CreateEntities : SyncRequestTask
    {
        /// <summary>container name the <see cref="entities"/> are created</summary>
        [Serialize                            ("cont")]
        [Required]  public  string              container;
        [Browse(Never)]
        [Ignore]    public  EntityContainer     entityContainer;
                    public  Guid?               reservedToken;
        /// <summary>name of the primary key property in <see cref="entities"/></summary>
                    public  string              keyName;
        /// <summary>the <see cref="entities"/> which are created in the specified <see cref="container"/></summary>
        [Serialize                            ("set")]
        [Required]  public  List<JsonEntity>    entities;
                        
        public   override   TaskType            TaskType => TaskType.create;
        public   override   string              TaskName => $"container: '{container}'";
        
        private TaskErrorResult PrepareCreate(
            EntityDatabase          database,
            SyncContext             syncContext,
            ref List<EntityError>   validationErrors)
        {
            if (container == null) {
                return MissingContainer();
            }
            if (entities == null) {
                return MissingField(nameof(entities));
            }
            if (!EntityUtils.GetKeysFromEntities(keyName, entities, syncContext, out string errorMsg)) {
                return InvalidTask(errorMsg);
            }
            entityContainer = database.GetOrCreateContainer(container);
            errorMsg = entityContainer.database.Schema?.ValidateEntities (container, entities, syncContext, EntityErrorType.WriteError, ref validationErrors);
            if (errorMsg != null) {
                return TaskError(new CommandError(TaskErrorResultType.ValidationError, errorMsg));
            }

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
            return null;
        }
        
        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            List<EntityError> validationErrors = null;
            var error =  PrepareCreate(database, syncContext, ref validationErrors);
            if (error != null) {
                return error;
            }
            if (entities.Count == 0) {
                return new CreateEntitiesResult{ errors = validationErrors };
            }
            var result = await entityContainer.CreateEntitiesAsync(this, syncContext).ConfigureAwait(false);
            
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            SyncResponse.AddEntityErrors(ref result.errors, validationErrors);
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            List<EntityError> validationErrors = null;
            var error = PrepareCreate(database, syncContext, ref validationErrors);
            if (error != null) {
                return error;
            }
            if (entities.Count == 0) {
                return new CreateEntitiesResult { errors = validationErrors };
            }
            var result = entityContainer.CreateEntities(this, syncContext);
            
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