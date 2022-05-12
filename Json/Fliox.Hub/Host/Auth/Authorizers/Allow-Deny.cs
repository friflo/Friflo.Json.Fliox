// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeAllow : Authorizer {
        private  readonly AuthorizeDatabase  authorizeDatabase;
            
        public override string ToString() => $"database: {authorizeDatabase.dbLabel}";

        public AuthorizeAllow (string database) {
            authorizeDatabase = new AuthorizeDatabase(database);
        }
        
        public override void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) => databases.Add(authorizeDatabase);
        
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            return authorizeDatabase.Authorize(executeContext);
        }
    }
    
    public sealed class AuthorizeDeny : Authorizer {
        
        public override void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) { }
        
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            return false;
        }
    }
}