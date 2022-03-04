// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class SendCommand : SyncMessageTask
    {
        internal override   TaskType        TaskType => TaskType.command;

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (name == null) {
                return MissingField(nameof(name));
            }
            if (database.handler.TryGetMessage(name, out var callback)) {
                var result  = await callback.InvokeCallback(name, param, executeContext).ConfigureAwait(false);
                if (result.error == null)
                    return new SendCommandResult { result = result.value };
                return new TaskErrorResult (TaskErrorResultType.CommandError, result.error);
            }
            var msg = $"no command handler for: '{name}'";
            return new TaskErrorResult (TaskErrorResultType.NotImplemented, msg);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class SendCommandResult : SyncMessageResult
    {
        public              JsonValue       result;

        internal override   TaskType        TaskType => TaskType.command;
    }
}