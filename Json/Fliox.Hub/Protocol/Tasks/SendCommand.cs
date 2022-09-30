// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Send a database command with the given <see cref="SyncMessageTask.param"/>. <br/>
    /// In case <see cref="SyncMessageTask.users"/> or <see cref="SyncMessageTask.clients"/> is set the Hub forward
    /// the message as an event only to the given <see cref="SyncMessageTask.users"/> or <see cref="SyncMessageTask.clients"/>.
    /// </summary>
    public sealed class SendCommand : SyncMessageTask
    {
        internal override   TaskType        TaskType => TaskType.command;

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (name == null) {
                return MissingField(nameof(name));
            }
            if (database.service.TryGetMessage(name, out var callback)) {
                var result  = await callback.InvokeDelegate(this, name, param, syncContext).ConfigureAwait(false);
                if (result.error == null)
                    return new SendCommandResult { result = result.value };
                return new TaskErrorResult (TaskErrorResultType.CommandError, result.error);
            }
            var msg = $"no command handler for: '{name}'";
            return new TaskErrorResult (TaskErrorResultType.NotImplemented, msg);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="SendCommand"/> task
    /// </summary>
    public sealed class SendCommandResult : SyncMessageResult
    {
        public              JsonValue       result;

        internal override   TaskType        TaskType => TaskType.command;
    }
}