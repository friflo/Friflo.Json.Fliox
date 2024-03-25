// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public delegate bool AuthPredicate (SyncRequestTask task, SyncContext syncContext);
    
    public sealed class AuthorizePredicate : TaskAuthorizer {
        private readonly string         name;
        private readonly AuthPredicate  predicate;
        public  override string         ToString() => name;

        public AuthorizePredicate (string name, AuthPredicate predicate) {
            this.name       = name;
            this.predicate  = predicate;    
        }
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) { }

        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            return predicate(task, syncContext);
        }
    }
}