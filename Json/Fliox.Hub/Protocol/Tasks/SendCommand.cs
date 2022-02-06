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

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (name == null) {
                return MissingField(nameof(name));
            }
            if (database.handler.TryGetCommand(name, out var callback)) {
                var jsonResult  = await callback.InvokeCallback(name, value, messageContext).ConfigureAwait(false);
                return new SendCommandResult { result = jsonResult };
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