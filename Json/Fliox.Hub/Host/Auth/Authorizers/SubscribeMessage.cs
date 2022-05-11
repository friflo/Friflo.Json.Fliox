// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeSubscribeMessage : IAuthorizer {
        private  readonly   AuthorizeDatabase   authorizeDatabase;
        private  readonly   string              messageName;
        private  readonly   bool                prefix;
        private  readonly   string              messageLabel;
        public   override   string              ToString() => $"database: {authorizeDatabase.dbLabel}, message: {messageLabel}";

        public AuthorizeSubscribeMessage (string message, string database) {
            authorizeDatabase   = new AuthorizeDatabase(database ?? EntityDatabase.MainDB);
            messageLabel        = message;
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) => databases.Add(authorizeDatabase);
        
        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            if (!authorizeDatabase.Authorize(executeContext))
                return false;
            if (!(task is SubscribeMessage subscribe))
                return false;
            if (prefix) {
                return subscribe.name.StartsWith(messageName);
            }
            return subscribe.name == messageName;
        }
    }
}