// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public abstract class SyncMessageTask : SyncRequestTask
    {
        [Fri.Required]  public  string          name;
                        public  JsonValue       value;
                        
        public   override       string          TaskName => $"name: '{name}'";
    }
    
    public sealed class SendMessage : SyncMessageTask
    {
        internal override       TaskType        TaskType => TaskType.message;

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (name == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(name)));
            SyncTaskResult result = new SendMessageResult();
            return Task.FromResult(result);
        }
    }

    // ----------------------------------- task result -----------------------------------
    public abstract class SyncMessageResult : SyncTaskResult, ICommandResult
    {
        public CommandError                 Error { get; set; }
    }
    
    public sealed class SendMessageResult : SyncMessageResult
    {
        internal override   TaskType        TaskType => TaskType.message;
    }
}