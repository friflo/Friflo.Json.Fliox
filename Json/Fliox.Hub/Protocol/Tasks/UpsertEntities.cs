// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
    /// Upsert the given <see cref="entities"/> in the specified <see cref="container"/>
    /// </summary>
    public sealed class UpsertEntities : SyncRequestTask
    {
        /// <summary>container name the <see cref="entities"/> are upserted - created or updated</summary>
        [Required]  public  string          container;
        /// <summary>name of the primary key property in <see cref="entities"/></summary>
                    public  string          keyName;
        /// <summary>the <see cref="entities"/> which are upserted in the specified <see cref="container"/></summary>
        [Required]  public  List<JsonValue> entities;
        
        [Ignore]    public  List<JsonKey>   entityKeys;
        /// <summary>if set the Hub forward the Upsert as an event only to given <see cref="users"/></summary>
                    public  List<JsonKey>   users;
        
        public   override   TaskType        TaskType => TaskType.upsert;
        public   override   string          TaskName => $"container: '{container}'";
        
        public override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            entityKeys = EntityUtils.GetKeysFromEntities(keyName, entities, syncContext, out string error);
            if (entityKeys == null) {
                return InvalidTask(error);
            }
            List<EntityError> validationErrors = null;
            error = database.Schema?.ValidateEntities (container, entityKeys, entities, syncContext, EntityErrorType.WriteError, ref validationErrors);
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
                        // if (json == null)
                        //     return InvalidTask("value of entities key/value elements not be null");
                        entities[n] = patcher.Copy(entity, true);
                    }
                }
            }
            await database.service.CustomizeUpsert(this, syncContext).ConfigureAwait(false);
            
            var result = await entityContainer.UpsertEntities(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            SyncResponse.AddEntityErrors(ref result.errors, validationErrors);
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="UpsertEntities"/> task
    /// </summary>
    public sealed class UpsertEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to upsert</summary>
                    public List<EntityError>   errors;

        internal override   TaskType            TaskType => TaskType.upsert;
    }
}