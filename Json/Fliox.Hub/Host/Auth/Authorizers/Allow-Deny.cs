// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeAllow : AuthorizerDatabase {
        public override string ToString() => $"database: {dbLabel}";

        public AuthorizeAllow () { }
        public AuthorizeAllow (string database) : base (database) { }
        
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            return AuthorizeDatabase(executeContext);
        }
    }
    
    public sealed class AuthorizeDeny : Authorizer {
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            return false;
        }
    }
}