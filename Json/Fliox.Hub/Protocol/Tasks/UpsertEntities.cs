// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
    /// Upsert the given <see cref="entities"/> in the specified <see cref="container"/>
    /// </summary>
    public sealed class UpsertEntities : SyncRequestTask
    {
        /// <summary>container name the <see cref="entities"/> are upserted - created or updated</summary>
        [Serialize                            ("cont")]
        [Required]  public  string              container;
        [Browse(Never)]
        [Ignore]   internal EntityContainer     entityContainer;
        [Ignore]   public   EntityContainer     EntityContainer => entityContainer;
        /// <summary>name of the primary key property in <see cref="entities"/></summary>
                   public   string              keyName;
        /// <summary>the <see cref="entities"/> which are upserted in the specified <see cref="container"/></summary>
        [Serialize                            ("set")]
        [Required] public   List<JsonEntity>    entities;
        
        /// <summary>if set the Hub forward the Upsert as an event only to given <see cref="users"/></summary>
        [Ignore]   public   List<JsonKey>       users;
        
        public   override   TaskType            TaskType => TaskType.upsert;
        public   override   string              TaskName => $"container: '{container}'";
        
        private TaskErrorResult PrepareUpsert(
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
            errorMsg = database.Schema?.ValidateEntities (container, entities, syncContext, EntityErrorType.WriteError, ref validationErrors);
            if (errorMsg != null) {
                return TaskError(new CommandError(TaskErrorResultType.ValidationError, errorMsg));
            }
            entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooled = syncContext.pool.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooled.instance;
                    for (int n = 0; n < entities.Count; n++) {
                        var entity = entities[n];
                        // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                        // if (json == null)
                        //     return InvalidTask("value of entities key/value elements not be null");
                        entities[n] = new JsonEntity(entity.key, patcher.Copy(entity.value, true));
                    }
                }
            }
            database.service.CustomizeUpsert(this, syncContext);
            return null;
        }
        
        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            List<EntityError> validationErrors = null;
            var error = PrepareUpsert(database, syncContext, ref validationErrors);
            if (error != null) {
                return error;
            }
            var result = await entityContainer.UpsertEntitiesAsync(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            SyncResponse.AddEntityErrors(ref result.errors, validationErrors);
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            List<EntityError> validationErrors = null;
            var error = PrepareUpsert(database, syncContext, ref validationErrors);
            if (error != null) {
                return error;
            }
            var result = entityContainer.UpsertEntities(this, syncContext);
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
        [Ignore]    public  CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to upsert</summary>
                    public  List<EntityError>   errors;
        
        public static UpsertEntitiesResult Create(SyncContext syncContext) {
            return syncContext.syncPools?.upsertResultPool.Create() ?? new UpsertEntitiesResult();
        }

        internal override   TaskType            TaskType => TaskType.upsert;
    }
}