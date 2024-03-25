// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Merge entities by id in the given <see cref="container"/><br/>
    /// </summary>
    public sealed class MergeEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Serialize                            ("cont")]
        [Required]  public  ShortString         container;
        [Browse(Never)]
        [Ignore]    public  EntityContainer     entityContainer;
        [Ignore]    private TaskErrorResult     error;
        /// <summary>name of the primary key property of the entity <see cref="patches"/></summary>
                    public  string              keyName;
        /// <summary>list of merge patches for each entity</summary>
        [Serialize                            ("set")]
        [Required]  public  List<JsonEntity>    patches;
        
        public   override   TaskType            TaskType => TaskType.merge;
        public   override   string              TaskName =>  $"container: '{container}'";
        public   override   bool                IsNop()  => patches.Count == 0;
        
        public override bool PreExecute(in PreExecute execute) {
            error = PrepareMerge(execute.db, execute.env);
            return base.PreExecute(execute);
        }
        
        private TaskErrorResult PrepareMerge(EntityDatabase database, SharedEnv env)
        {
            if (container.IsNull()) {
                return MissingContainer();
            }
            if (patches == null) {
                return MissingField(nameof(patches));
            }
            entityContainer = database.GetOrCreateContainer(container);
            if (entityContainer == null) {
                return ContainerNotFound();
            }
            var key = entityContainer.keyName ?? keyName;
            if (!KeyValueUtils.GetKeysFromEntities(key, patches, env, out string errorMsg)) {
                return InvalidTask(errorMsg);
            }
            return null;
        }

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            database.service.CustomizeMerge(this, syncContext);
            if (error != null) {
                return error;
            }
            if (IsNop()) {
                return new MergeEntitiesResult();
            }
            var result = await entityContainer.MergeEntitiesAsync(this, syncContext).ConfigureAwait(false);
            
            if (result.Error != null) {
                return TaskError(result.Error); 
            }
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            database.service.CustomizeMerge(this, syncContext);
            if (error != null) {
                return error;
            }
            if (IsNop()) {
                return new MergeEntitiesResult();
            }
            var result = entityContainer.MergeEntities(this, syncContext);
            
            if (result.Error != null) {
                return TaskError(result.Error); 
            }
            return result;
        }
    }

    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="MergeEntities"/> task
    /// </summary>
    public sealed class MergeEntitiesResult : SyncTaskResult, ITaskResultError
    {
        [Ignore]    public TaskExecuteError     Error { get; set; }
        /// <summary>list of entity errors failed to patch</summary>
                    public List<EntityError>    errors;
        
        internal override  TaskType             TaskType    => TaskType.merge;
        internal override   bool                Failed      => Error != null || errors != null;
    }
}