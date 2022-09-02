// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeDeny : Authorizer {
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) { }
        
        public override bool Authorize(SyncRequestTask task, SyncContext syncContext) {
            return false;
        }
    }
}