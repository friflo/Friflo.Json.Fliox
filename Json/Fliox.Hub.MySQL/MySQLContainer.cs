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
        private             bool            tableExists;
        public   override   bool            Pretty      { get; }
        private  readonly   MySQLDatabase   database;
        
        internal MySQLContainer(string name, MySQLDatabase database, bool pretty)
            : base(name, database)
        {
            Pretty          = pretty;
            this.database   = database;
        }

        private async Task<TaskExecuteError> EnsureContainerExists() {
            if (tableExists) {
                return null;
            }
            var sql = $"CREATE TABLE if not exists {name} (id VARCHAR(128), data TEXT);";
            await MySQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
            tableExists = true;
            return null;
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
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.MySQLFilter()}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                var result  = await MySQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            throw new NotImplementedException();
        }
        
       
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                await MySQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
                return new DeleteEntitiesResult();    
            }
            return new DeleteEntitiesResult();
        }
    }
}

#endif