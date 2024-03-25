// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Npgsql;
using static Friflo.Json.Fliox.Hub.PostgreSQL.PostgreSQLUtils;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseIndexFromEndExpression
// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    internal sealed partial class PostgreSQLContainer : EntityContainer, ISQLTable
    {
        internal readonly   TableInfo       tableInfo;
        internal readonly   TableType       tableType;
        
        internal PostgreSQLContainer(string name, PostgreSQLDatabase database)
            : base(name, database)
        {
            tableInfo   = new TableInfo (database, name, '"', '"', database.TableType);
            tableType   = database.TableType;
        }
        
        public async Task<SQLResult> CreateTable(ISyncConnection syncConnection)
        {
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
            }
            catch (NpgsqlException e) {
                return SQLResult.CreateError(e);
            }
        }
        
        private async Task<HashSet<string>> GetColumnNamesAsync(SyncConnection connection)
        {
            using var reader = await connection.ExecuteReaderAsync($"SELECT * FROM {name} LIMIT 0").ConfigureAwait(false);
            return await SQLUtils.GetColumnNamesAsync(reader).ConfigureAwait(false);
        }
        
        public async Task<SQLResult> AddVirtualColumns(ISyncConnection syncConnection)
        {
            if (tableType != TableType.JsonColumn) {
                return default;
            }
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
        
        public async Task<SQLResult> AddColumns (ISyncConnection syncConnection)
        {
            if (tableType != TableType.Relational) {
                return new SQLResult();
            }
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync (connection).ConfigureAwait(false);
            foreach (var column in tableInfo.columns) {
                if (columnNames.Contains(column.name)) {
                    continue;
                }
                var type    = ConvertContext.GetSqlType(column.type);
                var sql     = $"ALTER TABLE {name} ADD \"{column.name}\" {type};";
                var result  = await ExecuteAsync(connection, sql).ConfigureAwait(false);
                if (result.Failed) {
                    return result;
                }
            }
            return new SQLResult();
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            try {
                if (tableType == TableType.Relational) {
                    var sql = SQL.CreateRelational(this, command, syncContext);
                    await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                } else {
                    var sql = SQL.CreateJsonColumn(this, command);
                    await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                }
                return new CreateEntitiesResult();
            }
            catch (PostgresException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e) };    
            }
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            try {
                if (tableType == TableType.Relational) {
                    var sql = SQL.UpsertRelational(this, command, syncContext);
                    await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                } else {
                    var sql = SQL.UpsertJsonColumn(this, command);
                    await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                }
                return new UpsertEntitiesResult();
            }
            catch (PostgresException e) {
                return new UpsertEntitiesResult { Error = DatabaseError(e) };    
            }
        }

        /// <summary>sync version of <see cref="ReadEntities"/></summary>
        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (tableType == TableType.Relational) {
                    var sql             = SQL.ReadRelational(this, command);
                    using var reader    = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                    var mapper          = new PostgresSQL2Json(reader);
                    return await SQLTable.ReadEntitiesAsync(reader, mapper, command, tableInfo, syncContext).ConfigureAwait(false);
                } else {
                    var sql = SQL.ReadJsonColumn(this,command);
                    using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                    return await SQLUtils.ReadJsonColumnAsync(reader, command).ConfigureAwait(false);
                }
            }
            catch (PostgresException e) {
                return new ReadEntitiesResult { Error = DatabaseError(e) };
            }
        }

        /// <summary>sync version of <see cref="QueryEntities"/></summary>
        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var sql = SQL.Query(this, command);
            try {
                using var reader    = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                List<EntityValue> entities;
                if (tableType == TableType.Relational) {
                    using var pooled = syncContext.SQL2Json.Get();
                    var mapper  = new PostgresSQL2Json(reader);
                    var buffer  = syncContext.MemoryBuffer;
                    entities    = await mapper.ReadEntitiesAsync(pooled.instance, tableInfo, buffer).ConfigureAwait(false);
                } else {
                    entities = await SQLUtils.QueryJsonColumnAsync(reader).ConfigureAwait(false);
                }
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            }
            catch (PostgresException e) {
                return new QueryEntitiesResult { Error = DatabaseError(e), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var sql     = SQL.Count(this, command);
                var result  = await connection.ExecuteAsync(sql).ConfigureAwait(false);
                if (result.Failed) { return new AggregateEntitiesResult { Error = result.TaskError() }; }
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }
       
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error};
            }
            try {
                if (command.all == true) {
                    await connection.ExecuteNonQueryAsync($"DELETE from {name}").ConfigureAwait(false);
                    return new DeleteEntitiesResult();    
                }
                var sql = SQL.Delete(this, command);
                await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                return new DeleteEntitiesResult();
            }
            catch (PostgresException e) {
                return new DeleteEntitiesResult { Error = DatabaseError(e) };    
            }
        }
        
        private static TaskExecuteError DatabaseError(PostgresException e) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, e.MessageText );
        }
    }
}

#endif