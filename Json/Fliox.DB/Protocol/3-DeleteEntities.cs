// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- task -----------------------------------
    public sealed class DeleteEntities : SyncRequestTask
    {
        [Fri.Required]  public  string              container;
                        public  HashSet<JsonKey>    ids = new HashSet<JsonKey>(JsonKey.Equality);
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

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (ids == null && all == null)
                return MissingField($"[{nameof(ids)} | {nameof(all)}]");
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.DeleteEntities(this, messageContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            if (result.deleteErrors != null && result.deleteErrors.Count > 0) {
                var deleteErrors = SyncResponse.GetEntityErrors(ref response.deleteErrors, container);
                deleteErrors.AddErrors(result.deleteErrors);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class DeleteEntitiesResult : SyncTaskResult, ICommandResult
    {
                     public CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    deleteErrors;

        internal override   TaskType                        TaskType => TaskType.delete;
    }
}