// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeSubscribeChanges : TaskAuthorizer {
        private  readonly   DatabaseFilter      databaseFilter;
        private  readonly   ShortString         container;
        
        private  readonly   bool                create;
        private  readonly   bool                upsert;
        private  readonly   bool                delete;
        private  readonly   bool                merge;
        
        public   override   string  ToString() => $"database: {databaseFilter.dbLabel}, container: {container}";
        
        public AuthorizeSubscribeChanges (string container, ICollection<EntityChange> changes, string database)
        {
            databaseFilter  = new DatabaseFilter(database);
            this.container  = new ShortString(container);
            foreach (var change in changes) {
                switch (change) {
                    case EntityChange.create: create = true; break;
                    case EntityChange.upsert: upsert = true; break;
                    case EntityChange.delete: delete = true; break;
                    case EntityChange.merge:  merge  = true; break;
                }
            }
        }
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) => databaseFilters.Add(databaseFilter);
        
        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            if (!databaseFilter.Authorize(syncContext))
                return false;
            if (!(task is SubscribeChanges subscribe))
                return false;
            if (!subscribe.container.IsEqual(container))
                return false;
            var authorize = true;
            foreach (var change in subscribe.changes) {
                switch (change) {
                    case EntityChange.create:     authorize &= create;    break;
                    case EntityChange.upsert:     authorize &= upsert;    break;
                    case EntityChange.delete:     authorize &= delete;    break;
                    case EntityChange.merge:      authorize &= merge;     break;
                }
            }
            return authorize;
        }
    }
}