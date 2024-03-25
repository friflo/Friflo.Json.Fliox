// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using System.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;


// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal sealed partial class SQLServerContainer : EntityContainer, ISQLTable
    {
        internal readonly   TableInfo           tableInfo;
        public   override   bool                Pretty      { get; }
        internal readonly   TableType           tableType;
        
        // [Maximum capacity specifications for SQL Server - SQL Server | Microsoft Learn]
        // https://learn.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver16
        // Bytes per primary key 900, used 255 same as in MySQL
        internal const string ColumnId     = ID   + " NVARCHAR(255)";
        internal const string ColumnData   = DATA + " NVARCHAR(max)";
        
        internal SQLServerContainer(string name, SQLServerDatabase database, bool pretty)
            : base(name, database)
        {
            tableInfo   = new TableInfo (database, name, '[', ']', database.TableType);
            Pretty      = pretty;
            tableType   = database.TableType;
        }
        
        public async Task<SQLResult> CreateTable(ISyncConnection syncConnection) {
            var connection = (SyncConnection)syncConnection;
            if (tableType == TableType.JsonColumn) {
                var sql =
$@"IF NOT EXISTS (
    SELECT * FROM sys.tables t JOIN sys.schemas s ON (t.schema_id = s.schema_id)
    WHERE s.name = 'dbo' AND t.name = '{name}')
CREATE TABLE dbo.{name} ({ColumnId} PRIMARY KEY, {ColumnData});";
                return await connection.ExecuteAsync(sql).ConfigureAwait(false);
            } else {
                var sql =
$@"IF NOT EXISTS (
    SELECT * FROM sys.tables t JOIN sys.schemas s ON (t.schema_id = s.schema_id)
    WHERE s.name = 'dbo' AND t.name = '{name}')
CREATE TABLE dbo.{name}";
                var sb = new StringBuilder();
                sb.Append($"{sql} (");
                foreach (var column in tableInfo.columns) {
                    sb.Append('[');
                    sb.Append(column.name);
                    sb.Append("] ");
                    var type = ConvertContext.GetSqlType(column);
                    sb.Append(type);
                    if (column.isPrimaryKey) {
                        sb.Append(" PRIMARY KEY");
                    }
                    sb.Append(',');
                }
                sb.Length -= 1;
                sb.Append(");");
                return await connection.ExecuteAsync(sb.ToString()).ConfigureAwait(false);
            }
        }
        
        private async Task<HashSet<string>> GetColumnNamesAsync(SyncConnection connection)
        {
            using var reader = await connection.ExecuteReaderAsync($"SELECT TOP 0 * FROM {name}").ConfigureAwait(false);
            return await SQLUtils.GetColumnNamesAsync(reader).ConfigureAwait(false);
        }
        
        public async Task<SQLResult> AddVirtualColumns(ISyncConnection syncConnection)
        {
            if (tableType != TableType.JsonColumn) {
                return default;
            }
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync (connection).ConfigureAwait(false);
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
                return default;
            }
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync (connection).ConfigureAwait(false);
            foreach (var column in tableInfo.columns) {
                if (columnNames.Contains(column.name)) {
                    continue;
                }
                var type    = ConvertContext.GetSqlType(column);
                var sql     = $"ALTER TABLE {name} ADD [{column.name}] {type};";
                var result  = await connection.ExecuteAsync(sql).ConfigureAwait(false);
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
                var sql = new StringBuilder();
                if (tableType == TableType.Relational) {
                    CreateRelationalValues(sql, command.entities, tableInfo, syncContext); 
                    await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
                } else {
                    var p = CreateEntitiesCmdAsync(sql, command.entities, name);
                    await connection.ExecuteNonQueryAsync(sql.ToString(), p).ConfigureAwait(false);
                }
                return new CreateEntitiesResult();
            }
            catch (SqlException e) {
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
                var sql = new StringBuilder();
                if (tableType == TableType.Relational) {
                    UpsertRelationalValues(sql, command.entities, tableInfo, syncContext);
                    await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
                } else {
                    var p = UpsertEntitiesCmdAsync(sql, command.entities, name);
                    await connection.ExecuteNonQueryAsync(sql.ToString(), p).ConfigureAwait(false);
                }
                return new UpsertEntitiesResult();
            }
            catch (SqlException e) {
                return new UpsertEntitiesResult { Error = DatabaseError(e) };
            }
        }
        
        /// <summary>async version of <see cref="ReadEntities"/></summary>
        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (tableType == TableType.Relational) {
                    using var reader = await connection.ReadRelationalReaderAsync(tableInfo, command, syncContext).ConfigureAwait(false);
                    if (command.typeMapper != null) {
                        return await SQLTable.ReadObjectsAsync(reader, command, syncContext).ConfigureAwait(false);
                    }
                    var sql2Json = new SQL2JsonMapper(reader);
                    return await SQLTable.ReadEntitiesAsync(reader, sql2Json, command, tableInfo, syncContext).ConfigureAwait(false);
                } else {
                    var sql = SQL.ReadJsonColumn(this,command);
                    using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                    return await SQLUtils.ReadJsonColumnAsync(reader, command).ConfigureAwait(false);
                }
            }
            catch (SqlException e) {
                return new ReadEntitiesResult { Error = DatabaseError(e) };
            }
        }

        /// <summary>async version of <see cref="QueryEntities"/></summary>
        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var sql = SQL.Query(this, command);
            try {
                List<EntityValue> entities;
                using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                if (tableType == TableType.Relational) {
                    entities = await SQLTable.QueryEntitiesAsync(reader, tableInfo, syncContext).ConfigureAwait(false);
                } else {
                    entities = await SQLUtils.QueryJsonColumnAsync(reader).ConfigureAwait(false);
                }
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            }
            catch (SqlException e) {
                return new QueryEntitiesResult { Error = DatabaseError(e), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (command.type == AggregateType.count) {
                    var sql     = SQL.Count(this, command);
                    var result  = await connection.ExecuteAsync(sql).ConfigureAwait(false);
                    if (result.Failed) { return new AggregateEntitiesResult { Error = result.TaskError() }; }
                    return new AggregateEntitiesResult { value = (int)result.value };
                }
                return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
            }
            catch (SqlException e) {
                return new AggregateEntitiesResult { Error = DatabaseError(e) };
            }
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (command.all == true) {
                    await connection.ExecuteNonQueryAsync($"DELETE from {name}").ConfigureAwait(false);
                    return new DeleteEntitiesResult();
                }
                if (tableType == TableType.Relational) {
                    var sql = SQL.DeleteRelational(this, command);
                    await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                } else { 
                    var sql = new StringBuilder();
                    var p = DeleteEntitiesCmdAsync(sql, command.ids, name);
                    await connection.ExecuteNonQueryAsync(sql.ToString(), p).ConfigureAwait(false);
                }
                return new DeleteEntitiesResult();
            }
            catch (SqlException e) {
                return new DeleteEntitiesResult { Error = DatabaseError(e) };
            }
        }
        
        private static TaskExecuteError DatabaseError(SqlException e) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, GetErrMsg(e) );
        }
    }
}

#endif