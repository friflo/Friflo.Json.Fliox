// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class PatchEntities : DatabaseTask
    {
        public  string                          container;
        public  Dictionary<string, EntityPatch> patches;
        
        internal override   TaskType            TaskType => TaskType.patch;
        public   override   string              TaskName =>  $"container: '{container}'";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (patches == null)
                return MissingField(nameof(patches));
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.PatchEntities(this, messageContext).ConfigureAwait(false);
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
        public List<JsonPatch>                  patches;
    }

    // ----------------------------------- task result -----------------------------------
    public class PatchEntitiesResult : TaskResult, ICommandResult
    {
                     public CommandError                    Error { get; set; }
        [Fri.Ignore] public Dictionary<string, EntityError> patchErrors;
        
        internal override   TaskType                        TaskType => TaskType.patch;
    }
}