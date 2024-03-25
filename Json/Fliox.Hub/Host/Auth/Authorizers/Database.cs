// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeDatabase : TaskAuthorizer {
        private  readonly DatabaseFilter  databaseFilter;
            
        public override string ToString() => $"database: {databaseFilter.dbLabel}";

        public AuthorizeDatabase (string database) {
            databaseFilter = new DatabaseFilter(database);
        }
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) => databaseFilters.Add(databaseFilter);
        
        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            return databaseFilter.Authorize(syncContext);
        }
    }
}