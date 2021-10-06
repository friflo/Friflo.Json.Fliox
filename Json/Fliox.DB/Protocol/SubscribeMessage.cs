// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- task -----------------------------------
    public sealed class SubscribeMessage : SyncRequestTask
    {
        /// <summary>
        ///   Filter all <see cref="SendMessage.name"/>'s starting with one of the given <see cref="name"/> strings.
        ///   <para><see cref="name"/> = {""} => subscribe all message events.</para>
        ///   <para><see cref="name"/> = {} => unsubscribe message events.</para>
        /// </summary>
        [Fri.Required]  public  string      name;
                        public  bool?       remove;
        
        internal override       TaskType    TaskType    => TaskType.subscribeMessage;
        public   override       string      TaskName    => $"name: '{name}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("database has no eventBroker"));
            if (name == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(name)));
            var eventTarget = messageContext.eventTarget;
            if (eventTarget == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("caller/request doesnt provide a eventTarget"));
            
            if (messageContext.clientId.IsNull())
                return Task.FromResult<SyncTaskResult>(InvalidTask("invalid client id ('clt')"));
            eventBroker.SubscribeMessage(this, messageContext.clientId, eventTarget);
            return Task.FromResult<SyncTaskResult>(new SubscribeMessageResult());
        }
        
        internal static string GetPrefix (string name) {
            return name.EndsWith("*") ? name.Substring(0, name.Length - 1) : null;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class SubscribeMessageResult : SyncTaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeMessage;
    }
}