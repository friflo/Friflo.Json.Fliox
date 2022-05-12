// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public delegate bool AuthPredicate (SyncRequestTask task, ExecuteContext executeContext);
    
    public sealed class AuthorizePredicate : Authorizer {
        private readonly string         name;
        private readonly AuthPredicate  predicate;
        public  override string         ToString() => name;

        public AuthorizePredicate (string name, AuthPredicate predicate) {
            this.name       = name;
            this.predicate  = predicate;    
        }
        
        public override void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) { }

        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            return predicate(task, executeContext);
        }
    }
}