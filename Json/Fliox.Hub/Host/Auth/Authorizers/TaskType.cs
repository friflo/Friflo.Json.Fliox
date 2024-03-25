// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeTaskType : TaskAuthorizer {
        private  readonly   DatabaseFilter  databaseFilter;
        private  readonly   TaskType        type;
        
        public   override   string      ToString() => $"database: {databaseFilter.dbLabel}, type: {type.ToString()}";

        public AuthorizeTaskType(TaskType type, string database) {
            databaseFilter  = new DatabaseFilter(database);
            this.type       = type;    
        }
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) => databaseFilters.Add(databaseFilter);
        
        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            if (!databaseFilter.Authorize(syncContext))
                return false;
            return task.TaskType == type;
        }
    }
}