// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SubscribeMessages : DatabaseTask
    {
        /// <summary>
        ///   Filter all <see cref="Message.name"/>'s starting with one of the given <see cref="tags"/> strings.
        ///   <para><see cref="tags"/> = {""} => subscribe all message events.</para>
        ///   <para><see cref="tags"/> = {} => unsubscribe message events.</para>
        /// </summary>
        public              List<string>    tags;
        internal override   TaskType        TaskType    => TaskType.subscribeMessages;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<TaskResult>(InvalidTask("database has no eventBroker"));
            if (messageContext.clientId == null)
                return Task.FromResult<TaskResult>(InvalidTask("subscribe task requires client id set in sync request"));
            if (tags == null)
                return Task.FromResult<TaskResult>(MissingField(nameof(tags)));
            
            var eventTarget = messageContext.eventTarget;
            if (eventTarget == null)
                return Task.FromResult<TaskResult>(InvalidTask("caller/request doesnt provide a eventTarget"));
            
            eventBroker.SubscribeMessages(this, messageContext.clientId, eventTarget);
            return Task.FromResult<TaskResult>(new SubscribeMessagesResult());
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SubscribeMessagesResult : TaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeMessages;
    }
}