// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        [Required]  public  ShortString         container;
        [Browse(Never)]
        [Ignore]   public   EntityContainer     entityContainer;
        [Ignore]   private  List<EntityError>   validationErrors;
        [Ignore]   private  TaskErrorResult     error;
        /// <summary>name of the primary key property in <see cref="entities"/></summary>
                   public   string              keyName;
        /// <summary>the <see cref="entities"/> which are upserted in the specified <see cref="container"/></summary>
        [Serialize                            ("set")]
        [Required] public   List<JsonEntity>    entities;
        
        public   override   TaskType            TaskType => TaskType.upsert;
        public   override   string              TaskName => $"container: '{container}'";
        public   override   bool                IsNop()  => entities.Count == 0;
        
        public override bool PreExecute(in PreExecute execute) {
            error = PrepareUpsert(execute.db, execute.env);
            return base.PreExecute(execute);
        }
        
        private TaskErrorResult PrepareUpsert(EntityDatabase database, SharedEnv env)
        {
            validationErrors = null;
            if (container.IsNull()) {
                return MissingContainer();
            }
            if (entities == null) {
                return MissingField(nameof(entities));
            }
            entityContainer = database.GetOrCreateContainer(container);
            if (entityContainer == null) {
                return ContainerNotFound();
            }
            var key = entityContainer.keyName ?? keyName;
            if (!KeyValueUtils.GetKeysFromEntities(key, entities, env, out string errorMsg)) {
                return InvalidTask(errorMsg);
            }
            errorMsg = database.Schema?.ValidateEntities (container, entities, env, EntityErrorType.WriteError, ref validationErrors);
            if (errorMsg != null) {
                return TaskError(new TaskExecuteError(TaskErrorType.ValidationError, errorMsg));
            }
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooled = env.pool.JsonPatcher.Get()) {
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
            return null;
        }
        
        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            database.service.CustomizeUpsert(this, syncContext);
            if (error != null) {
                return error;
            }
            if (IsNop()) {
                return UpsertEntitiesResult.Create(syncContext, validationErrors);
            }
            var result = await entityContainer.UpsertEntitiesAsync(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            SyncResponse.AddEntityErrors(ref result.errors, validationErrors);
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            database.service.CustomizeUpsert(this, syncContext);
            if (error != null) {
                return error;
            }
            if (IsNop()) {
                return UpsertEntitiesResult.Create(syncContext, validationErrors);
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
    public sealed class UpsertEntitiesResult : SyncTaskResult, ITaskResultError
    {
        [Ignore]    public  TaskExecuteError    Error { get; set; }
        /// <summary>list of entity errors failed to upsert</summary>
                    public  List<EntityError>   errors;
        
        public static UpsertEntitiesResult Create(SyncContext syncContext, List<EntityError> entityErrors) {
            var result = syncContext.syncPools?.upsertResultPool.Create() ?? new UpsertEntitiesResult();
            result.errors = entityErrors;
            return result;
        }

        internal override   TaskType            TaskType    => TaskType.upsert;
        internal override   bool                Failed      => Error != null || errors != null;
    }
}