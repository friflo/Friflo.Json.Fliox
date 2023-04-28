// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;


namespace Friflo.Json.Fliox.Hub.MySQL
{
    public sealed class MySQLContainer : EntityContainer
    {
        // private          bool        tableExists;
        public   override   bool        Pretty      { get; }
        
        internal MySQLContainer(string name, MySQLDatabase database, bool pretty)
            : base(name, database)
        {
            Pretty      = pretty;
        }

        private bool EnsureContainerExists(out TaskExecuteError error) {
            error = null;
            return false;
        }
        
        public override Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var result = CreateEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var result = UpsertEntities(command, syncContext);
            return Task.FromResult(result);
        }

        public override Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var result = ReadEntities(command, syncContext);
            return Task.FromResult(result);
        }

       
        public override Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var result = QueryEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            throw new NotImplementedException();
        }
        
       
        public override Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var result = DeleteEntities(command, syncContext);
            return Task.FromResult(result);
        }
    }
}

#endif