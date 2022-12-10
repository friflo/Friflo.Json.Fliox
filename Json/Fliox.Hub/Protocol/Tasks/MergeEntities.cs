// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
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
        [Required]  public  string              container {
            get => containerSmall.value;
            set => containerSmall = new SmallString(value);
        }
    
        [Browse(Never)]
        [Ignore]   internal SmallString         containerSmall;
        /// <summary>name of the primary key property of the entity <see cref="patches"/></summary>
                    public  string              keyName;
        /// <summary>list of merge patches for each entity</summary>
        [Required]  public  List<JsonEntity>    patches;
        
        /// <summary>if set the Hub forward the Merge as an event only to given <see cref="users"/></summary>
        [Ignore]    public  List<JsonKey>       users;
        
        public   override   TaskType            TaskType => TaskType.merge;
        public   override   string              TaskName =>  $"container: '{container}'";
        
        private EntityContainer PrepareMerge(
            EntityDatabase      database,
            SyncContext         syncContext,
            out TaskErrorResult error
            )
        {
            if (container == null) {
                error = MissingContainer();
                return null;
            }
            if (patches == null) {
                error = MissingField(nameof(patches));
                return null;
            }
            database.service.CustomizeMerge(this, syncContext);
            error = null;
            return database.GetOrCreateContainer(container);
        }

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var entityContainer = PrepareMerge(database, syncContext, out var error);
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
            var entityContainer = PrepareMerge(database, syncContext, out var error);
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