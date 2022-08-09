// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeSubscribeChanges : Authorizer {
        private  readonly   AuthorizeDatabase   authorizeDatabase;
        private  readonly   string              container;
        
        private  readonly   bool                create;
        private  readonly   bool                upsert;
        private  readonly   bool                delete;
        private  readonly   bool                patch;
        
        public   override   string  ToString() => $"database: {authorizeDatabase.dbLabel}, container: {container}";
        
        public AuthorizeSubscribeChanges (string container, ICollection<EntityChange> changes, string database)
        {
            authorizeDatabase   = new AuthorizeDatabase(database);
            this.container      = container;
            foreach (var change in changes) {
                switch (change) {
                    case EntityChange.create: create = true; break;
                    case EntityChange.upsert: upsert = true; break;
                    case EntityChange.delete: delete = true; break;
                    case EntityChange.patch:  patch  = true; break;
                }
            }
        }
        
        public override void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) => databases.Add(authorizeDatabase);
        
        public override bool Authorize(SyncRequestTask task, SyncContext syncContext) {
            if (!authorizeDatabase.Authorize(syncContext))
                return false;
            if (!(task is SubscribeChanges subscribe))
                return false;
            if (subscribe.container != container)
                return false;
            var authorize = true;
            foreach (var change in subscribe.changes) {
                switch (change) {
                    case EntityChange.create:     authorize &= create;    break;
                    case EntityChange.upsert:     authorize &= upsert;    break;
                    case EntityChange.delete:     authorize &= delete;    break;
                    case EntityChange.patch:      authorize &= patch;     break;
                }
            }
            return authorize;
        }
    }
}