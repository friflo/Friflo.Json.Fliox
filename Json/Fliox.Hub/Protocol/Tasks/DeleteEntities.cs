// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Delete entities by id in the given <see cref="container"/><br/>
    /// The entities which will be deleted are listed in <see cref="ids"/>
    /// </summary>
    public sealed class DeleteEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Serialize                            ("cont")]
        [Required]  public  ShortString         container;
        [Browse(Never)]
        [Ignore]    public  EntityContainer     entityContainer;
        [Ignore]    private TaskErrorResult     error;
        /// <summary>list of <see cref="ids"/> requested for deletion</summary>
                    public  ListOne<JsonKey>    ids;
        /// <summary>if true all entities in the specified <see cref="container"/> are deleted</summary>
                    public  bool?               all;
        
        public   override   TaskType            TaskType => TaskType.delete;
        public   override   string              TaskName => $"container: '{container}'";
        public   override   bool                IsNop()  => ids?.Count == 0;
        
        internal bool Authorize (in ShortString container, bool delete, bool deleteAll) {
            bool allBool = all != null && all.Value;
            if (delete    && ids.Count >  0 && !allBool     && this.container.IsEqual(container))
                return true;
            if (deleteAll && ids.Count == 0 && allBool      && this.container.IsEqual(container))
                return true;
            return false;
        }
        
        public override bool PreExecute(in PreExecute execute) {
            error = PrepareDelete(execute.db);
            return base.PreExecute(execute);
        }
        
        private TaskErrorResult PrepareDelete(EntityDatabase database)
        {
            if (container.IsNull()) {
                return MissingContainer();
            }
            if (ids == null && all == null) {
                return MissingField($"[{nameof(ids)} | {nameof(all)}]");
            }
            entityContainer = database.GetOrCreateContainer(container);
            if (entityContainer == null) {
                return ContainerNotFound();
            }
            return null;
        }

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            database.service.CustomizeDelete(this, syncContext);
            if (error != null) {
                return error;
            }
            if (IsNop()) {
                return new DeleteEntitiesResult();
            }
            var result = await entityContainer.DeleteEntitiesAsync(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            database.service.CustomizeDelete(this, syncContext);
            if (error != null) {
                return error;
            }
            if (IsNop()) {
                return new DeleteEntitiesResult();
            }
            var result = entityContainer.DeleteEntities(this, syncContext);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="DeleteEntities"/> task
    /// </summary>
    public sealed class DeleteEntitiesResult : SyncTaskResult, ITaskResultError
    {
        [Ignore]    public  TaskExecuteError    Error { get; set; }
        /// <summary>list of entity errors failed to delete</summary>
                    public  List<EntityError>   errors;

        internal override   TaskType            TaskType    => TaskType.delete;
        internal override   bool                Failed      => Error != null || errors != null;
    }
}