// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Sync
{
    public class SubscribeChanges : DatabaseTask
    {
        public string                   container;
        public HashSet<TaskType>        types;
        public FilterOperation          filter;
        
        internal override   TaskType    TaskType    => TaskType.subscribe;

        public   override   string      ToString()  => container;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<TaskResult>(InvalidTask("database has no eventBroker"));
            
            var messageTarget = syncContext.eventTarget;
            if (messageTarget == null)
                return Task.FromResult<TaskResult>(InvalidTask("caller/request doesnt provide a messageTarget"));
            
            eventBroker.Subscribe(this, messageTarget);
            return Task.FromResult<TaskResult>(new SubscribeChangesResult());
        }
    }
    
    public class SubscribeChangesResult : TaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribe;
    }
}