// Copyright (c) Ullrich Praetz. All rights reserved.
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
    /// Merge entities by id in the given <see cref="container"/><br/>
    /// </summary>
    public sealed class MergeEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Serialize                            ("cont")]
        [Required]  public  string              container;
        [Browse(Never)]
        [Ignore]   internal EntityContainer     entityContainer;
        [Ignore]    public  EntityContainer     EntityContainer => entityContainer;
        /// <summary>name of the primary key property of the entity <see cref="patches"/></summary>
                    public  string              keyName;
        /// <summary>list of merge patches for each entity</summary>
        [Serialize                            ("set")]
        [Required]  public  List<JsonEntity>    patches;
        
        /// <summary>if set the Hub forward the Merge as an event only to given <see cref="users"/></summary>
        [Ignore]    public  List<JsonKey>       users;
        
        public   override   TaskType            TaskType => TaskType.merge;
        public   override   string              TaskName =>  $"container: '{container}'";
        
        private TaskErrorResult PrepareMerge(
            EntityDatabase      database,
            SyncContext         syncContext)
        {
            if (container == null) {
                return MissingContainer();
            }
            if (patches == null) {
                return MissingField(nameof(patches));
            }
            database.service.CustomizeMerge(this, syncContext);
            entityContainer = database.GetOrCreateContainer(container);
            return null;
        }

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var error = PrepareMerge(database, syncContext);
            if (error != null) {
                return error;
            }
            var result = await entityContainer.MergeEntitiesAsync(this, syncContext).ConfigureAwait(false);
            
            if (result.Error != null) {
                return TaskError(result.Error); 
            }
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var error = PrepareMerge(database, syncContext);
            if (error != null) {
                return error;
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
    public sealed class MergeEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to patch</summary>
                    public List<EntityError>   errors;
        
        internal override  TaskType            TaskType => TaskType.merge;
    }
}