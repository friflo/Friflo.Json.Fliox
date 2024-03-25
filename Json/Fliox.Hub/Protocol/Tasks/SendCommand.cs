// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        public   override   TaskType        TaskType => TaskType.command;
        
        
        private TaskErrorResult PrepareSend() {
            if (name.IsNull()) {
                return MissingField(nameof(name));
            }
            if (callback == null) {
                var msg = $"no command handler for: '{name.AsString()}'";
                return new TaskErrorResult (TaskErrorType.NotImplemented, msg);
            }
            return null;
        }

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var error = PrepareSend();
            if (error != null) {
                return error;
            }
            var result  = await callback.InvokeDelegateAsync(this, syncContext).ConfigureAwait(false);
            if (result.Success) {
                return new SendCommandResult { result = result.value };
            }
            return new TaskErrorResult (result.error);
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var error = PrepareSend();
            if (error != null) {
                return error;
            }
            var result  = callback.InvokeDelegate(this, syncContext);
            if (result.Success) {
                return new SendCommandResult { result = result.value };
            }
            return new TaskErrorResult (result.error);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="SendCommand"/> task
    /// </summary>
    public sealed class SendCommandResult : SyncMessageResult
    {
        public              JsonValue       result;

        internal override   TaskType        TaskType    => TaskType.command;
        internal override   bool            Failed      => false;
    }
}