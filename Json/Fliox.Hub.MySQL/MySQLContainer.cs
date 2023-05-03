// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using MySqlConnector;

// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
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
            var sql = $"CREATE TABLE if not exists {name} (id VARCHAR(128) PRIMARY KEY, data JSON);";
            var result = await MySQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
            if (!result.Success) {
                return result.error;
            }
            tableExists = true;
            return null;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new CreateEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {name} (id,data) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities);
            using var cmd = new MySqlCommand(sql.ToString(), database.connection);
            try {
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            } catch (Exception e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"REPLACE INTO {name} (id,data) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities);
            using var cmd = new MySqlCommand(sql.ToString(), database.connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new ReadEntitiesResult { Error = error };
            }
            var ids = command.ids;
            var sql = new StringBuilder();
            sql.Append($"SELECT id, data FROM {name} WHERE id in\n");
            SQLUtils.AppendKeysSQL(sql, ids);
            using var cmd   = new MySqlCommand(sql.ToString(), database.connection);
            return await SQLUtils.ReadEntities(cmd, command).ConfigureAwait(false);
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new QueryEntitiesResult { Error = error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.MySQLFilter(database.Provider);
            var sql     = SQLUtils.QueryEntitiesSQL(command, name, where);
            try {
                using var cmd = new MySqlCommand(sql, database.connection);
                return await SQLUtils.QueryEntities(cmd, command, sql).ConfigureAwait(false);
            }
            catch (MySqlException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.MySQLFilter(database.Provider)}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                var result  = await MySQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
                if (!result.Success) { return new AggregateEntitiesResult { Error = result.error }; }
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            throw new NotImplementedException();
        }
        
       
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new DeleteEntitiesResult { Error = error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                var result = await MySQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
                if (!result.Success) { return new DeleteEntitiesResult { Error = result.error }; }
                return new DeleteEntitiesResult();    
            } else {
                var sql = new StringBuilder();
                sql.Append($"DELETE FROM  {name} WHERE id in\n");
                
                SQLUtils.AppendKeysSQL(sql, command.ids);
                using var cmd = new MySqlCommand(sql.ToString(), database.connection);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                return new DeleteEntitiesResult();
            }
        }
        
        private static TaskExecuteError DatabaseError(string message) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, message);
        }
    }
}

#endif