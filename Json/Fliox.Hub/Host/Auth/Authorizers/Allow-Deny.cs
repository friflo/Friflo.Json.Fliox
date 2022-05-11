// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeAllow : IAuthorizer {
        private  readonly AuthorizeDatabase  authorizeDatabase;
            
        public override string ToString() => $"database: {authorizeDatabase.dbLabel}";

        public AuthorizeAllow () {
            authorizeDatabase = new AuthorizeDatabase("*");
        }
        
        public AuthorizeAllow (string database) {
            authorizeDatabase = new AuthorizeDatabase(database);
        }
        
        public void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) => databases.Add(authorizeDatabase);
        
        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            return authorizeDatabase.Authorize(executeContext);
        }
    }
    
    public sealed class AuthorizeDeny : IAuthorizer {
        
        public void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) { }
        
        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            return false;
        }
    }
}