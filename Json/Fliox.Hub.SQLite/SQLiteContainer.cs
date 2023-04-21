// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteContainer : EntityContainer
    {
        public   override   bool                Pretty      { get; }
        
        internal SQLiteContainer(string name, EntityDatabase database, bool pretty)
            : base(name, database)
        {
            Pretty          = pretty;
        }

        private Task EnsureContainerExists() {
            return Task.CompletedTask;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            throw new NotImplementedException();
        }

        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            throw new NotImplementedException();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            throw new NotImplementedException();
        }
        
        // private readonly bool filterByClient = false; // true: used for development => query all and filter thereafter
        
        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            throw new NotImplementedException();
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            throw new NotImplementedException();
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            throw new NotImplementedException();
        }
    }
}

#endif