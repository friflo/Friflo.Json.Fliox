// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Merge entities by id in the given <see cref="container"/><br/>
    /// </summary>
    public sealed class MergeEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Required]  public  string              container;
        /// <summary>name of the primary key property of the entity <see cref="patches"/></summary>
                    public  string              keyName;
        /// <summary>list of merge patches for each entity</summary>
        [Required]  public  List<JsonValue>     patches = new List<JsonValue>();
        
        /// <summary>if set the Hub forward the Merge as an event only to given <see cref="users"/></summary>
        [Ignore]    public  List<JsonKey>       users;
        
        public   override   TaskType            TaskType => TaskType.merge;
        public   override   string              TaskName =>  $"container: '{container}'";

        public override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container == null)
                return MissingContainer();
            if (patches == null)
                return MissingField(nameof(patches));
            var entityContainer = database.GetOrCreateContainer(container);
            
            await database.service.CustomizeMerge(this, syncContext).ConfigureAwait(false);
            
            var result = await entityContainer.MergeEntities(this, syncContext).ConfigureAwait(false);
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
    public sealed class MergeEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to patch</summary>
                    public List<EntityError>   errors;
        
        internal override  TaskType            TaskType => TaskType.merge;
    }
}