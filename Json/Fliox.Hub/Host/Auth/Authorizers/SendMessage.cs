// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeSendMessage : TaskAuthorizer {
        private  readonly   DatabaseFilter      databaseFilter;
        private  readonly   ShortString         messageName;
        private  readonly   bool                prefix;
        private  readonly   string              messageLabel;
        public   override   string              ToString() => $"database: {databaseFilter.dbLabel}, message: {messageLabel}";

        public AuthorizeSendMessage (string message, string database) {
            databaseFilter  = new DatabaseFilter(database);
            messageLabel    = message;
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = new ShortString(message.Substring(0, message.Length - 1));
                return;
            }
            messageName = new ShortString(message);
        }
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) => databaseFilters.Add(databaseFilter);
        
        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            if (!databaseFilter.Authorize(syncContext))
                return false;
            if (!(task is SyncMessageTask messageTask))
                return false;
            if (prefix) {
                return messageTask.name.StartsWith(messageName);
            }
            return messageTask.name.IsEqual(messageName);
        }
    }
}