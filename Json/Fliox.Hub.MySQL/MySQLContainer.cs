// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.MySQL.MySQLUtils;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal sealed class MySQLContainer : EntityContainer, ISQLTable
    {
        private  readonly   TableInfo       tableInfo;
        public   override   bool            Pretty      { get; }
        private  readonly   MySQLProvider   provider;
        private  readonly   TableType       tableType;
        
        internal MySQLContainer(string name, MySQLDatabase database, bool pretty)
            : base(name, database)
        {
            tableInfo   = new TableInfo (database, name, '`', '`', database.TableType);
            Pretty      = pretty;
            provider    = database.Provider;
            tableType   = database.TableType;
        }
        
        public async Task<SQLResult> CreateTable(ISyncConnection syncConnection) {
            var connection = (SyncConnection)syncConnection;
            if (tableType == TableType.JsonColumn) {
                // [MySQL :: MySQL 8.0 Reference Manual :: 11.7 Data Type Storage Requirements] https://dev.mysql.com/doc/refman/8.0/en/storage-requirements.html
                var sql = $"CREATE TABLE if not exists {name} ({ID} VARCHAR(255) PRIMARY KEY, {DATA} JSON);";
                return await Execute(connection, sql).ConfigureAwait(false);
            }
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE if not exists {name} (");
            foreach (var column in tableInfo.columns) {
                sb.Append('`');
                sb.Append(column.name);
                sb.Append("` ");
                var type = ConvertContext.GetSqlType(column, provider);
                sb.Append(type);
                if (column.isPrimaryKey) {
                    sb.Append(" PRIMARY KEY");
                }
                sb.Append(',');
            }
            sb.Length -= 1;
            sb.Append(");");
            return await Execute(connection, sb.ToString()).ConfigureAwait(false);
        }
        
        private async Task<HashSet<string>> GetColumnNamesAsync(SyncConnection connection) {
            using var reader = await connection.ExecuteReaderAsync($"SELECT * FROM {name} LIMIT 0").ConfigureAwait(false);
            return await SQLUtils.GetColumnNamesAsync(reader).ConfigureAwait(false);
        }
        
        public async Task<SQLResult> AddVirtualColumns(ISyncConnection syncConnection) {
            if (tableType != TableType.JsonColumn) {
                return default;
            }
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync(connection).ConfigureAwait(false);
            foreach (var column in tableInfo.columns) {
                if (column == tableInfo.keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                var result = await AddVirtualColumn(connection, name, column, provider).ConfigureAwait(false);
                if (result.Failed) {
                    return result;
                }
            }
            return new SQLResult();
        }
        
        public async Task<SQLResult> AddColumns (ISyncConnection syncConnection) {
            if (tableType != TableType.Relational) {
                return default;
            }
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync (connection).ConfigureAwait(false);
            foreach (var column in tableInfo.columns) {
                if (columnNames.Contains(column.name)) {
                    continue;
                }
                var type    = ConvertContext.GetSqlType(column, provider);
                var sql     = $"ALTER TABLE {name} ADD `{column.name}` {type};";
                var result  = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) {
                    return result;
                }
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
                SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash, tableInfo, syncContext);
            } else {
                sql.Append($"INSERT INTO {name} ({ID},{DATA})\nVALUES ");
                SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash);
            }
            try {
                await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
            } catch (MySqlException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e) };
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
                sql.Append($"REPLACE INTO {name}");
                SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash, tableInfo, syncContext);
            } else {
                sql.Append($"REPLACE INTO {name} ({ID},{DATA})\nVALUES");
                SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash);
            }
            try {
                await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
            } catch (MySqlException e) {
                return new UpsertEntitiesResult { Error = DatabaseError(e) };
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
                sql.Append($" FROM {name} WHERE {tableInfo.keyColumn.name} in\n");
            } else {
                sql.Append($"SELECT {ID}, {DATA} FROM {name} WHERE {ID} in\n");
            }
            SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.BackSlash);
            try {
                using var reader = await connection.ExecuteReaderAsync(sql.ToString()).ConfigureAwait(false);
                if (tableType == TableType.Relational) {
                    return await SQLTable.ReadEntitiesAsync(reader, command, tableInfo, syncContext).ConfigureAwait(false);
                } else {
                    return await SQLUtils.ReadEntitiesAsync(reader, command).ConfigureAwait(false);
                }
            } catch (MySqlException e) {
                return new ReadEntitiesResult { Error = DatabaseError(e) };
            }
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.MySQLFilter(provider, tableType);
            var sql     = SQLUtils.QueryEntitiesSQL(command, name, where, tableInfo);
            try {
                using var reader    = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                List<EntityValue> entities;
                if (tableType == TableType.Relational) {
                    entities    = await SQLTable.QueryEntitiesAsync(reader, tableInfo, syncContext).ConfigureAwait(false);
                } else {
                    entities    = await SQLUtils.QueryEntitiesAsync(reader).ConfigureAwait(false);
                }
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            }
            catch (MySqlException e) {
                var msg = GetErrMsg(e);
                return new QueryEntitiesResult { Error = new TaskExecuteError(msg), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.MySQLFilter(provider, tableType)}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";

                var result  = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new AggregateEntitiesResult { Error = result.TaskError() }; }
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                var result = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new DeleteEntitiesResult { Error = result.TaskError() }; }
                return new DeleteEntitiesResult();    
            } else {
                var sql = new StringBuilder();
                var id = tableType == TableType.Relational ? tableInfo.keyColumn.name : ID;
                sql.Append($"DELETE FROM  {name} WHERE {id} in\n");
                SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.BackSlash);
                try {
                    await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
                } catch (MySqlException e) {
                    return new DeleteEntitiesResult { Error = DatabaseError(e) };
                }
                return new DeleteEntitiesResult();
            }
        }
        
        private static TaskExecuteError DatabaseError(MySqlException exception) {
            var msg = GetErrMsg(exception);
            return new TaskExecuteError(TaskErrorType.DatabaseError, msg);
        }
    }
}

#endif