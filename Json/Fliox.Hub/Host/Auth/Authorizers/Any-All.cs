// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeAny : Authorizer {
        internal  readonly  IReadOnlyList<Authorizer>     list;
        
        public AuthorizeAny(IReadOnlyList<Authorizer> list) {
            this.list = list;    
        }
        
        public override void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) {
            foreach (var item in list) {
                item.AddAuthorizedDatabases(databases);
            }
        }

        public override bool Authorize(SyncRequestTask task, SyncContext syncContext) {
            foreach (var item in list) {
                if (item.Authorize(task, syncContext))
                    return true;
            }
            return false;
        }
    }
    
    public sealed class AuthorizeAll : Authorizer {
        private readonly    ICollection<Authorizer>     list;
        
        public AuthorizeAll(ICollection<Authorizer> list) {
            this.list = list;    
        }
        
        public override void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) {
            foreach (var item in list) {
                item.AddAuthorizedDatabases(databases);
            }
        }
        
        public override bool Authorize(SyncRequestTask task, SyncContext syncContext) {
            foreach (var item in list) {
                if (!item.Authorize(task, syncContext))
                    return false;
            }
            return true;
        }
    }
}