// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
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
    internal sealed class PostgreSQLContainer : EntityContainer, ISQLTable
    {
        private  readonly   TableInfo       tableInfo;
        private  readonly   TypeDef         entityType;
        private  readonly   TableType       tableType;
        
        internal PostgreSQLContainer(string name, PostgreSQLDatabase database)
            : base(name, database)
        {
            tableInfo   = new TableInfo (database, name, '"', '"', database.TableType);
            var types   = database.Schema.typeSchema.GetEntityTypes();
            entityType  = types[name];
            tableType   = database.TableType;
        }
        
        public async Task<SQLResult> CreateTable(ISyncConnection syncConnection) {
            var connection = (SyncConnection)syncConnection;
            try {
                if (tableType == TableType.JsonColumn) {
                    // [PostgreSQL primary key length limit - Stack Overflow] https://stackoverflow.com/questions/4539443/postgresql-primary-key-length-limit
                    // "The maximum length for a value in a B-tree index, which includes primary keys, is one third of the size of a buffer page, by default floor(8192/3) = 2730 bytes."
                    // set to 255 as for all SQL databases
                    var sql = $"CREATE TABLE if not exists {name} ({ID} VARCHAR(255) PRIMARY KEY, {DATA} JSONB);";
                    return await ExecuteAsync(connection, sql).ConfigureAwait(false);
                }
                var sb = new StringBuilder();
                sb.Append($"CREATE TABLE if not exists {name} (");
                foreach (var column in tableInfo.columns) {
                    sb.Append('"');
                    sb.Append(column.name);
                    sb.Append("\" ");
                    var type = ConvertContext.GetSqlType(column.type);
                    sb.Append(type);
                    if (column.isPrimaryKey) {
                        sb.Append(" PRIMARY KEY");
                    }
                    sb.Append(',');
                }
                sb.Length -= 1;
                sb.Append(");");
                return await ExecuteAsync(connection, sb.ToString()).ConfigureAwait(false);
            } catch (NpgsqlException e) {
                return SQLResult.CreateError(e);
            }
        }
        
        private async Task<HashSet<string>> GetColumnNamesAsync(SyncConnection connection) {
            using var reader = await connection.ExecuteReaderAsync($"SELECT * FROM {name} LIMIT 0").ConfigureAwait(false);
            return await SQLUtils.GetColumnNamesAsync(reader).ConfigureAwait(false);
        }
        
        public async Task<SQLResult> AddVirtualColumns(ISyncConnection syncConnection) {
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync(connection).ConfigureAwait(false);
            foreach (var column in tableInfo.columns) {
                if (column == tableInfo.keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                var result = await AddVirtualColumn(connection, name, column).ConfigureAwait(false);
                if (result.Failed) {
                    return result;
                }
            }
            return new SQLResult();
        }
        
        public async Task<SQLResult> AddColumns (ISyncConnection syncConnection) {
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync (connection).ConfigureAwait(false);
            foreach (var column in tableInfo.columns) {
                if (columnNames.Contains(column.name)) {
                    continue;
                }
                // ...
            }
            return new SQLResult();
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            var sql = new StringBuilder();
            if (tableType == TableType.Relational) {
                sql.Append($"INSERT INTO {name}");
                SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool, tableInfo, syncContext);
            } else {
                sql.Append($"INSERT INTO {name} ({ID},{DATA}) VALUES\n");
                SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool);
            }
            try {
                await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
            } catch (NpgsqlException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            var sql = new StringBuilder();
            if (tableType == TableType.Relational) {
                sql.Append($"INSERT INTO {name}");
                SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool, tableInfo, syncContext);
                sql.Append($"\nON CONFLICT(\"{tableInfo.keyColumn.name}\") DO UPDATE SET "); // {DATA} = excluded.{DATA};");
                foreach (var column in tableInfo.columns) {
                    sql.Append('"'); sql.Append(column.name); sql.Append("\"=excluded.\""); sql.Append(column.name); sql.Append("\", ");
                }
                sql.Length -= 2;
                sql.Append(';');
            } else {
                sql.Append($"INSERT INTO {name} ({ID},{DATA}) VALUES\n");
                SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool);
                sql.Append($"\nON CONFLICT({ID}) DO UPDATE SET {DATA} = excluded.{DATA};");
            }
            try {
                await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
            } catch (NpgsqlException e) {
                return new UpsertEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            var sql = new StringBuilder();
            if (tableType == TableType.Relational) {
                sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, tableInfo);
                sql.Append($" FROM {name} WHERE \"{tableInfo.keyColumn.name}\" in\n");
            } else {
                sql.Append($"SELECT {ID}, {DATA} FROM {name} WHERE {ID} in\n");
            }
            SQLUtils.AppendKeysSQL(sql,  command.ids, SQLEscape.Default);
            try {
                using var reader = await connection.ExecuteReaderAsync(sql.ToString()).ConfigureAwait(false);
                if (tableType == TableType.Relational) {
                    using var pooled = syncContext.SQL2Json.Get();
                    var mapper   = new PostgresSQL2Json(reader);
                    var buffer   = syncContext.MemoryBuffer;
                    var entities = await mapper.ReadEntitiesAsync(pooled.instance, tableInfo, buffer).ConfigureAwait(false);
                    var array    = KeyValueUtils.EntityListToArray(entities, command.ids);
                    return new ReadEntitiesResult { entities = array };
                } else {
                    return await SQLUtils.ReadEntitiesAsync(reader, command).ConfigureAwait(false);
                }
            } catch (NpgsqlException e) {
                return new ReadEntitiesResult { Error = new TaskExecuteError(e.Message) };
            }
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.PostgresFilter(entityType, tableType);
            var sql     = SQLUtils.QueryEntitiesSQL(command, name, where, tableInfo);
            try {
                using var reader    = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                List<EntityValue> entities;
                if (tableType == TableType.Relational) {
                    using var pooled = syncContext.SQL2Json.Get();
                    var mapper  = new PostgresSQL2Json(reader);
                    var buffer  = syncContext.MemoryBuffer;
                    entities    = await mapper.ReadEntitiesAsync(pooled.instance, tableInfo, buffer).ConfigureAwait(false);
                } else {
                    entities = await SQLUtils.QueryEntitiesAsync(reader).ConfigureAwait(false);
                }
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            } catch (NpgsqlException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.PostgresFilter(entityType, tableType)}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                var result  = await ExecuteAsync(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new AggregateEntitiesResult { Error = result.TaskError() }; }
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }
        
       
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error};
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                var result = await ExecuteAsync(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new DeleteEntitiesResult { Error = result.TaskError() }; }
                return new DeleteEntitiesResult();    
            } else {
                var sql = new StringBuilder();
                var id = tableType == TableType.Relational ? tableInfo.keyColumn.name : ID;
                sql.Append($"DELETE FROM  {name} WHERE {id} in\n");
                SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.Default);
                try {
                    await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
                } catch (NpgsqlException e) {
                    return new DeleteEntitiesResult { Error = DatabaseError(e.Message) };    
                }
                return new DeleteEntitiesResult();
            }
        }
        
        private static TaskExecuteError DatabaseError(string message) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, message);
        }
    }
}

#endif