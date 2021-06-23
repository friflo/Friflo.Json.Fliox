// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SubscribeEcho : DatabaseTask
    {
        public              List<string>    prefixes;
        internal override   TaskType        TaskType    => TaskType.subscribeEcho;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<TaskResult>(InvalidTask("database has no eventBroker"));
            if (syncContext.clientId == null)
                return Task.FromResult<TaskResult>(InvalidTask("subscribe task requires client id set in sync request"));
            if (prefixes == null)
                return Task.FromResult<TaskResult>(MissingField(nameof(prefixes)));
            
            var eventTarget = syncContext.eventTarget;
            if (eventTarget == null)
                return Task.FromResult<TaskResult>(InvalidTask("caller/request doesnt provide a eventTarget"));
            
            eventBroker.SubscribeEcho(this, syncContext.clientId, eventTarget);
            return Task.FromResult<TaskResult>(new SubscribeEchoResult());
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SubscribeEchoResult : TaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeEcho;
    }
}