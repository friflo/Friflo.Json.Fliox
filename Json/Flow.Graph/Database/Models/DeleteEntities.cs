// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Friflo.Json.Flow.Database.Models
{
    public class DeleteEntities : DatabaseTask
    {
        public              string              container;
        public              HashSet<string>     ids;
        
        internal override   TaskType            TaskType => TaskType.Delete;
        public   override   string              ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            return await entityContainer.DeleteEntities(this);
        }
    }
    
    public class DeleteEntitiesResult : TaskResult
    {
        internal override TaskType      TaskType => TaskType.Delete;
    }
}