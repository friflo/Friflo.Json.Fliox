// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Patch entities by id in the given <see cref="container"/><br/>
    /// Each <see cref="EntityPatch"/> in <see cref="patches"/> contains a set of <see cref="patches"/> for each entity. 
    /// </summary>
    public sealed class PatchEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Required]  public  string              container;
        /// <summary>name of the primary key property of the entity <see cref="patches"/></summary>
                    public  string              keyName;
        /// <summary>list of patches for each entity</summary>
        [Required]  public  List<EntityPatch>   patches = new List<EntityPatch>();
        
        public   override   TaskType            TaskType => TaskType.patch;
        public   override   string              TaskName =>  $"container: '{container}'";
        
        public override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container == null)
                return MissingContainer();
            if (patches == null)
                return MissingField(nameof(patches));
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.PatchEntities(this, response, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error); 
            }
            return result;
        }
    }

    /// <summary>
    /// Contains the <see cref="patches"/> applied to an entity. Used by <see cref="PatchEntities"/>
    /// </summary>
    public sealed class EntityPatch
    {
        [Required]  public  JsonKey             id;
        /// <summary>list of patches applied to an entity</summary>
        [Required]  public  List<JsonPatch>     patches;

        public override     string              ToString() => id.AsString();
    }

    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="PatchEntities"/> task
    /// </summary>
    public sealed class PatchEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to patch</summary>
                    public List<EntityError>   errors;
        
        internal override  TaskType            TaskType => TaskType.patch;
    }
}