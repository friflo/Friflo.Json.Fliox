// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    public class DeleteEntities : DatabaseTask
    {
        public              string              container;
        public              HashSet<string>     ids;
        
        internal override   TaskType            TaskType => TaskType.Delete;
        public   override   string              ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.DeleteEntities(this);
            var deleteError = result.Error;
            if (deleteError != null) {
                return new TaskError {type = TaskErrorType.DatabaseError, message = deleteError.message};
            }
            return result;
        }
    }
    
    public class DeleteEntitiesResult : TaskResult, ICommandResult
    {
        public              DatabaseError       Error { get; set; }

        internal override   TaskType            TaskType => TaskType.Delete;
    }
}