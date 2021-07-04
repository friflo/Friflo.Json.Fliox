// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SubscribeMessage : DatabaseTask
    {
        /// <summary>
        ///   Filter all <see cref="SendMessage.name"/>'s starting with one of the given <see cref="name"/> strings.
        ///   <para><see cref="name"/> = {""} => subscribe all message events.</para>
        ///   <para><see cref="name"/> = {} => unsubscribe message events.</para>
        /// </summary>
        public              string          name;
        public              bool?           remove;
        
        internal override   TaskType        TaskType    => TaskType.subscribeMessage;
        public   override   string          TaskName    => $"name: {name}";

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<TaskResult>(InvalidTask("database has no eventBroker"));
            if (messageContext.clientId == null)
                return Task.FromResult<TaskResult>(InvalidTask("subscribe task requires client id set in sync request"));
            if (name == null)
                return Task.FromResult<TaskResult>(MissingField(nameof(name)));
            
            var eventTarget = messageContext.eventTarget;
            if (eventTarget == null)
                return Task.FromResult<TaskResult>(InvalidTask("caller/request doesnt provide a eventTarget"));
            
            eventBroker.SubscribeMessage(this, messageContext.clientId, eventTarget);
            return Task.FromResult<TaskResult>(new SubscribeMessageResult());
        }
        
        internal static string GetPrefix (string name) {
            return name.EndsWith("*") ? name.Substring(0, name.Length - 1) : null;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SubscribeMessageResult : TaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeMessage;
    }
}