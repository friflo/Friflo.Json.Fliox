// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
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
        [Required]  public  string              container {
            get => containerSmall.value;
            set => containerSmall = new SmallString(value);
        }
        [Browse(Never)]
        [Ignore]   internal SmallString         containerSmall;
        /// <summary>name of the primary key property in <see cref="entities"/></summary>
                   public   string              keyName;
        /// <summary>the <see cref="entities"/> which are upserted in the specified <see cref="container"/></summary>
        [Required] public   List<JsonEntity>    entities;
        
        /// <summary>if set the Hub forward the Upsert as an event only to given <see cref="users"/></summary>
        [Ignore]   public   List<JsonKey>       users;
        
        public   override   TaskType            TaskType => TaskType.upsert;
        public   override   string              TaskName => $"container: '{container}'";
        
        private EntityContainer PrepareUpsert(
            EntityDatabase          database,
            SyncContext             syncContext,
            ref List<EntityError>   validationErrors,
            out TaskErrorResult     error)
        {
            if (container == null) {
                error = MissingContainer();
                return null;
            }
            if (entities == null) {
                error = MissingField(nameof(entities));
                return null;
            }
            if (!EntityUtils.GetKeysFromEntities(keyName, entities, syncContext, out string errorMsg)) {
                error = InvalidTask(errorMsg);
                return null;
            }
            errorMsg = database.Schema?.ValidateEntities (container, entities, syncContext, EntityErrorType.WriteError, ref validationErrors);
            if (errorMsg != null) {
                error = TaskError(new CommandError(TaskErrorResultType.ValidationError, errorMsg));
                return null;
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
                        entities[n] = new JsonEntity(entity.key, patcher.Copy(entity.value, true));
                    }
                }
            }
            database.service.CustomizeUpsert(this, syncContext);
            error = null;
            return entityContainer;
        }
        
        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            List<EntityError> validationErrors = null;
            var entityContainer = PrepareUpsert(database, syncContext, ref validationErrors, out var error);
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
            var entityContainer = PrepareUpsert(database, syncContext, ref validationErrors, out var error);
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