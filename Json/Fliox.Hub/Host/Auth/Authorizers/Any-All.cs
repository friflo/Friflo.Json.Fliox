// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeAny : IAuthorizer {
        private readonly    ICollection<IAuthorizer>     list;
        
        public AuthorizeAny(ICollection<IAuthorizer> list) {
            this.list = list;    
        }
        
        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            foreach (var item in list) {
                if (item.Authorize(task, executeContext))
                    return true;
            }
            return false;
        }
    }
    
    public sealed class AuthorizeAll : IAuthorizer {
        private readonly    ICollection<IAuthorizer>     list;
        
        public AuthorizeAll(ICollection<IAuthorizer> list) {
            this.list = list;    
        }
        
        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            foreach (var item in list) {
                if (!item.Authorize(task, executeContext))
                    return false;
            }
            return true;
        }
    }
}