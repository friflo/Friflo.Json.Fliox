// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public class SendMessage : SyncRequestTask
    {
        [Fri.Required]  public  string          name;
                        public  JsonValue       value;
            
        internal override       TaskType        TaskType => TaskType.message;
        public   override       string          TaskName => $"name: '{name}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (name == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(name)));
            SyncTaskResult result = new SendMessageResult();
            return Task.FromResult(result);
        }
    }
    
    public sealed class SendCommand : SendMessage
    {
        internal override       TaskType        TaskType => TaskType.command;
        public   override       string          TaskName => $"name: '{name}'";

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (name == null) {
                return MissingField(nameof(name));
            }
            if (database.taskHandler.TryGetCommand(name, out var callback)) {
                var jsonResult  = await callback.InvokeCallback(name, value, messageContext).ConfigureAwait(false);
                return new SendCommandResult { result = jsonResult };
            }
            var msg = $"command handler not found: '{name}'";
            return new TaskErrorResult { type = TaskErrorResultType.NotImplemented, message = msg };
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SendMessageResult : SyncTaskResult, ICommandResult
    {
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.message;
    }


    public sealed class SendCommandResult : SendMessageResult
    {
        public              JsonValue       result;

        internal override   TaskType        TaskType => TaskType.command;
    }
}