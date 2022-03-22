// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeSubscribeChanges : IAuthorizer {
        private  readonly   AuthorizeDatabase   authorizeDatabase;
        private  readonly   string              container;
        
        private  readonly   bool                create;
        private  readonly   bool                upsert;
        private  readonly   bool                delete;
        private  readonly   bool                patch;
        
        public   override   string  ToString() => $"database: {authorizeDatabase.dbLabel}, container: {container}";
        
        public AuthorizeSubscribeChanges (string container, ICollection<Change> changes, string database)
        {
            authorizeDatabase   = new AuthorizeDatabase(database);
            this.container      = container;
            foreach (var change in changes) {
                switch (change) {
                    case Change.create: create = true; break;
                    case Change.upsert: upsert = true; break;
                    case Change.delete: delete = true; break;
                    case Change.patch:  patch  = true; break;
                }
            }
        }
        
        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            if (!authorizeDatabase.Authorize(executeContext))
                return false;
            if (!(task is SubscribeChanges subscribe))
                return false;
            if (subscribe.container != container)
                return false;
            var authorize = true;
            foreach (var change in subscribe.changes) {
                switch (change) {
                    case Change.create:     authorize &= create;    break;
                    case Change.upsert:     authorize &= upsert;    break;
                    case Change.delete:     authorize &= delete;    break;
                    case Change.patch:      authorize &= patch;     break;
                }
            }
            return authorize;
        }
    }
}