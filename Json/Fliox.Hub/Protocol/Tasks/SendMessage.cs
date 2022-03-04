// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public abstract class SyncMessageTask : SyncRequestTask
    {
        [Fri.Required]  public  string          name;
                        public  JsonValue       param;
                        
        public   override       string          TaskName => $"name: '{name}'";
    }
    
    public sealed class SendMessage : SyncMessageTask
    {
        internal override       TaskType        TaskType => TaskType.message;

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (name == null)
                return MissingField(nameof(name));
            if (database.handler.TryGetMessage(name, out var callback)) {
                var result  = await callback.InvokeCallback(name, param, executeContext).ConfigureAwait(false); // todo could be synchronous call
                if (result.error != null) {
                    return new TaskErrorResult (TaskErrorResultType.CommandError, result.error);
                }
            }
            return new SendMessageResult();
        }
    }

    // ----------------------------------- task result -----------------------------------
    public abstract class SyncMessageResult : SyncTaskResult, ICommandResult
    {
        [Fri.Ignore] public CommandError    Error { get; set; }
    }
    
    public sealed class SendMessageResult : SyncMessageResult
    {
        internal override   TaskType        TaskType => TaskType.message;
    }
}