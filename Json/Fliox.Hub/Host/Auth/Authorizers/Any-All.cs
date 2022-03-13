// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeAny : Authorizer {
        private readonly    ICollection<Authorizer>     list;
        
        public AuthorizeAny(ICollection<Authorizer> list) {
            this.list = list;    
        }
        
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            foreach (var item in list) {
                if (item.Authorize(task, executeContext))
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
        
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            foreach (var item in list) {
                if (!item.Authorize(task, executeContext))
                    return false;
            }
            return true;
        }
    }
}