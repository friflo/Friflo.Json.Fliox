// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeGrant : TaskAuthorizer
    {
        public override         string          ToString() => $"database: *";
        
        private static readonly DatabaseFilter  DatabaseFilter = new DatabaseFilter("*");
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) => databaseFilters.Add(DatabaseFilter);
        
        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            return true;
        }
    }
}