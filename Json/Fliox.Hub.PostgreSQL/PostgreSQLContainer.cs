// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using Npgsql;
using static Friflo.Json.Fliox.Hub.PostgreSQL.PostgreSQLUtils;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseIndexFromEndExpression
// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public sealed class PostgreSQLContainer : EntityContainer, ISQLTable
    {
        private  readonly   TableInfo       tableInfo;
        private  readonly   ContainerInit   init;
        private  readonly   TypeDef         entityType;
        
        internal PostgreSQLContainer(string name, PostgreSQLDatabase database)
            : base(name, database)
        {
            init            = new ContainerInit(database);
            tableInfo       = new TableInfo (database, name);
            var types       = database.Schema.typeSchema.GetEntityTypes();
            entityType      = types[name];
        }
        
        public async Task<TaskExecuteError> InitTable(ISyncConnection connection) {
            if (init.CreateTable) {
                // [PostgreSQL primary key length limit - Stack Overflow] https://stackoverflow.com/questions/4539443/postgresql-primary-key-length-limit
                // "The maximum length for a value in a B-tree index, which includes primary keys, is one third of the size of a buffer page, by default floor(8192/3) = 2730 bytes."
                // set to 255 as for all SQL databases
                var sql = $"CREATE TABLE if not exists {name} ({ID} VARCHAR(255) PRIMARY KEY, {DATA} JSONB);";
                var result = await Execute((SyncConnection)connection, sql).ConfigureAwait(false);
                if (result.Failed) {
                    return result.error;
                }
                init.tableCreated = true;
            }
            if (init.AddVirtualColumns) {
                await AddVirtualColumns(connection).ConfigureAwait(false);
                init.virtualColumnsAdded = true;
            }
            return null;
        }
        
        public async Task AddVirtualColumns(ISyncConnection syncConnection) {
            var connection = (SyncConnection)syncConnection;
            using var cmd   = Command($"SELECT * FROM {name} LIMIT 0", connection);
            var columnNames = await SQLUtils.GetColumnNamesAsync(cmd).ConfigureAwait(false);
            foreach (var column in tableInfo.columns.Values) {
                if (column == tableInfo.keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                await AddVirtualColumn(connection, name, column).ConfigureAwait(false);
            }
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new CreateEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {name} ({ID},{DATA}) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.Default);
            try {
                using var cmd = Command(sql.ToString(), connection);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            } catch (Exception e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {name} ({ID},{DATA}) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.Default);
            sql.Append($"\nON CONFLICT({ID}) DO UPDATE SET {DATA} = excluded.{DATA};");
            using var cmd = Command(sql.ToString(), connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new ReadEntitiesResult { Error = error };
            }
            var sql = new StringBuilder();
            sql.Append($"SELECT {ID}, {DATA} FROM {name} WHERE {ID} in\n");
            SQLUtils.AppendKeysSQL(sql,  command.ids, SQLEscape.Default);
            using var cmd = Command(sql.ToString(), connection);
            return await SQLUtils.ReadEntitiesAsync(cmd, command).ConfigureAwait(false);
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new QueryEntitiesResult { Error = error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.PostgresFilter(entityType);
            var sql     = SQLUtils.QueryEntitiesSQL(command, name, where);
            try {
                using var cmd = Command(sql, connection);
                var entities = await SQLUtils.QueryEntitiesAsync(cmd).ConfigureAwait(false);
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            } catch (PostgresException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.MessageText), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.PostgresFilter(entityType)}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                var result  = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new AggregateEntitiesResult { Error = result.error }; }
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }
        
       
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error};
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new DeleteEntitiesResult { Error = error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                var result = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new DeleteEntitiesResult { Error = result.error }; }
                return new DeleteEntitiesResult();    
            } else {
                var sql = new StringBuilder();
                sql.Append($"DELETE FROM  {name} WHERE {ID} in\n");
                
                SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.Default);
                using var cmd = Command(sql.ToString(), connection);
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