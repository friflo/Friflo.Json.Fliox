// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeSendMessage : AuthorizerDatabase {
        private  readonly   string      messageName;
        private  readonly   bool        prefix;
        private  readonly   string      messageLabel;
        public   override   string      ToString() => $"database: {dbLabel}, message: {messageLabel}";

        public AuthorizeSendMessage (string message, string database) : base (database) {
            messageLabel = message;
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            if (!AuthorizeDatabase(executeContext))
                return false;
            if (!(task is SyncMessageTask messageTask))
                return false;
            if (prefix) {
                return messageTask.name.StartsWith(messageName);
            }
            return messageTask.name == messageName;
        }
    }
}