// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeSendMessage : Authorizer {
        private  readonly   AuthorizeDatabase   authorizeDatabase;
        private  readonly   string              messageName;
        private  readonly   bool                prefix;
        private  readonly   string              messageLabel;
        public   override   string              ToString() => $"database: {authorizeDatabase.dbLabel}, message: {messageLabel}";

        public AuthorizeSendMessage (string message, string database) {
            authorizeDatabase   = new AuthorizeDatabase(database);
            messageLabel        = message;
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public override void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) => databases.Add(authorizeDatabase);
        
        public override bool Authorize(SyncRequestTask task, SyncContext syncContext) {
            if (!authorizeDatabase.Authorize(syncContext))
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