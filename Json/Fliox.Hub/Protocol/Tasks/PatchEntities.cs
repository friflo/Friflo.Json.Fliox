// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Transform;
using Req = Friflo.Json.Fliox.RequiredMemberAttribute;
using Ignore = Friflo.Json.Fliox.IgnoreMemberAttribute;

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
        [Req]       public  string                              container;
        /// <summary>name of the primary key property of the entity <see cref="patches"/></summary>
                    public  string                              keyName;
        /// <summary>set of patches for each entity identified by its primary key</summary>
        [Req]       public  Dictionary<JsonKey, EntityPatch>    patches = new Dictionary<JsonKey, EntityPatch>(JsonKey.Equality);
        
        internal override   TaskType                            TaskType => TaskType.patch;
        public   override   string                              TaskName =>  $"container: '{container}'";
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
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
        /// <summary>list of patches applied to an entity</summary>
        [Req]  public  List<JsonPatch>      patches;
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