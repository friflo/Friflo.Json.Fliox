// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SubscribeChanges : DatabaseTask
    {
        public string                   container;
        public HashSet<Change>          changes;
        public FilterOperation          filter;
        
        internal override   TaskType    TaskType    => TaskType.subscribe;

        public   override   string      ToString()  => container;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<TaskResult>(InvalidTask("database has no eventBroker"));
            if (syncContext.clientId == null)
                return Task.FromResult<TaskResult>(InvalidTask("subscribe task requires client id set in sync request"));
            if (container == null)
                return Task.FromResult<TaskResult>(MissingContainer());
            if (changes == null)
                return Task.FromResult<TaskResult>(MissingField(nameof(changes)));
            
            var eventTarget = syncContext.eventTarget;
            if (eventTarget == null)
                return Task.FromResult<TaskResult>(InvalidTask("caller/request doesnt provide a eventTarget"));
            
            eventBroker.SubscribeChanges(this, syncContext.clientId, eventTarget);
            return Task.FromResult<TaskResult>(new SubscribeChangesResult());
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SubscribeChangesResult : TaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribe;
    }
    
    // ReSharper disable InconsistentNaming
    public enum Change
    {
        create,
        update,
        patch,
        delete
    }
}