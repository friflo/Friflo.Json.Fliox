// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
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
        [Fri.Required]  public  string                              container;
                        public  string                              keyName;
        [Fri.Required]  public  Dictionary<JsonKey, EntityPatch>    patches = new Dictionary<JsonKey, EntityPatch>(JsonKey.Equality);
        
        internal override       TaskType                            TaskType => TaskType.patch;
        public   override       string                              TaskName =>  $"container: '{container}'";
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (container == null)
                return MissingContainer();
            if (patches == null)
                return MissingField(nameof(patches));
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.PatchEntities(this, response, executeContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error); 
            }
            if (result.patchErrors != null && result.patchErrors.Count > 0) {
                var patchErrors = SyncResponse.GetEntityErrors(ref response.patchErrors, container);
                patchErrors.AddErrors(result.patchErrors);
            }
            return result;
        }
    }

    public class EntityPatch
    {
        [Fri.Required]  public  List<JsonPatch>             patches;
    }

    // ----------------------------------- task result -----------------------------------
    public sealed class PatchEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Fri.Ignore] public CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    patchErrors = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        
        internal override   TaskType                            TaskType => TaskType.patch;
    }
}