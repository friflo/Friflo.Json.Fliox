// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;

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
        [Required]  public  string              container {
            get => containerSmall.value;
            set => containerSmall = new SmallString(value);
        }
        [Ignore]   internal SmallString         containerSmall;
        /// <summary>list of <see cref="ids"/> requested for deletion</summary>
                    public  List<JsonKey>       ids;
        /// <summary>if true all entities in the specified <see cref="container"/> are deleted</summary>
                    public  bool?               all;
        
        public   override   TaskType            TaskType => TaskType.delete;
        public   override   string              TaskName => $"container: '{container}'";
        
        internal bool Authorize (string container, bool delete, bool deleteAll) {
            bool allBool = all != null && all.Value;
            if (delete    && ids.Count >  0 && !allBool        && this.container == container)
                return true;
            if (deleteAll && ids.Count == 0 && allBool         && this.container == container)
                return true;
            return false;
        }
        
        private EntityContainer PrepareDelete(
            EntityDatabase          database,
            SyncContext             syncContext,
            out TaskErrorResult     error)
        {
            if (container == null) {
                error = MissingContainer();
                return null;
            }
            if (ids == null && all == null) {
                error = MissingField($"[{nameof(ids)} | {nameof(all)}]");
                return null;
            }
            error           = null;
            return database.GetOrCreateContainer(container);
        }

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var entityContainer = PrepareDelete (database, syncContext, out var error);
            if (error != null) {
                return error;
            }
            var result = await entityContainer.DeleteEntitiesAsync(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var entityContainer = PrepareDelete (database, syncContext, out var error);
            if (error != null) {
                return error;
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
    public sealed class DeleteEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public  CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to delete</summary>
                    public  List<EntityError>   errors;

        internal override   TaskType            TaskType => TaskType.delete;
    }
}