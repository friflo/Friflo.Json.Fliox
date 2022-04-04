// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

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
        [Fri.Required]  public  string              container;
        /// <summary>list of <see cref="ids"/> requested for deletion</summary>
                        public  HashSet<JsonKey>    ids = new HashSet<JsonKey>(JsonKey.Equality);
        /// <summary>if true all entities in the specified <see cref="container"/> are deleted</summary>
                        public  bool?               all;
        
        internal override       TaskType            TaskType => TaskType.delete;
        public   override       string              TaskName => $"container: '{container}'";
        
        internal bool Authorize (string container, bool delete, bool deleteAll) {
            bool allBool = all != null && all.Value;
            if (delete    && ids.Count >  0 && !allBool        && this.container == container)
                return true;
            if (deleteAll && ids.Count == 0 && allBool         && this.container == container)
                return true;
            return false;
        }

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (container == null)
                return MissingContainer();
            if (ids == null && all == null)
                return MissingField($"[{nameof(ids)} | {nameof(all)}]");
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.DeleteEntities(this, executeContext).ConfigureAwait(false);
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
        [Fri.Ignore] public CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to delete</summary>
                     public List<EntityError>   errors;

        internal override   TaskType            TaskType => TaskType.delete;
    }
}