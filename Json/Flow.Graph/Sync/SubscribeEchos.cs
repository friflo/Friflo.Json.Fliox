// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SubscribeEchos : DatabaseTask
    {
        /// <summary>
        ///   Filter all <see cref="Echo.message"/>'s starting with one of the given <see cref="prefixes"/> strings.
        ///   <para><see cref="prefixes"/> = {""} => subscribe all echo events.</para>
        ///   <para><see cref="prefixes"/> = {} => unsubscribe echos events.</para>
        /// </summary>
        public              List<string>    prefixes;
        internal override   TaskType        TaskType    => TaskType.subscribeEchos;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<TaskResult>(InvalidTask("database has no eventBroker"));
            if (messageContext.clientId == null)
                return Task.FromResult<TaskResult>(InvalidTask("subscribe task requires client id set in sync request"));
            if (prefixes == null)
                return Task.FromResult<TaskResult>(MissingField(nameof(prefixes)));
            
            var eventTarget = messageContext.eventTarget;
            if (eventTarget == null)
                return Task.FromResult<TaskResult>(InvalidTask("caller/request doesnt provide a eventTarget"));
            
            eventBroker.SubscribeEchos(this, messageContext.clientId, eventTarget);
            return Task.FromResult<TaskResult>(new SubscribeEchoResult());
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SubscribeEchoResult : TaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeEchos;
    }
}