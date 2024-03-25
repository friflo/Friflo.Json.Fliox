// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using SQLitePCL;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;
using static Friflo.Json.Fliox.Hub.SQLite.SQLiteUtils;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal sealed class SQLiteContainer : EntityContainer, ISQLTable
    {
        // --- public
        private  readonly   TableInfo       tableInfo;
        private  readonly   bool            synchronous;
        public   override   bool            Pretty      { get; }
        // --- private
        private  readonly   TableType       tableType;
        private  readonly   ColumnInfo[]    columns;
        private  readonly   ColumnInfo      keyColumn;
        
        internal SQLiteContainer(string name, SQLiteDatabase database, bool pretty)
            : base(name, database)
        {
            tableInfo   = new TableInfo (database, name, '"', '"', database.TableType);
            columns     = tableInfo.columns;
            keyColumn   = tableInfo.keyColumn;
            synchronous = database.Synchronous;
            Pretty      = pretty;
            tableType   = database.TableType;
        }

        public Task<SQLResult> CreateTable(ISyncConnection syncConnection) {
            var connection = (SyncConnection)syncConnection;
            if (tableType == TableType.JsonColumn) {
                var sql = $"CREATE TABLE IF NOT EXISTS {name} ({ID} TEXT PRIMARY KEY, {DATA} TEXT NOT NULL);";
                return Task.FromResult(Execute(connection, sql));
            }
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE if not exists {name} (");
            foreach (var column in columns) {
                var type = ConvertContext.GetSqlType(column);
                sb.Append($"`{column.name}` {type}");
                if (column.isPrimaryKey) {
                    sb.Append(" PRIMARY KEY");
                }
                sb.Append(',');
            }
            sb.Length -= 1;
            sb.Append(");");
            var result = Execute(connection, sb.ToString());
            return Task.FromResult(result);
        }
        
        public Task<SQLResult> AddVirtualColumns(ISyncConnection syncConnection) {
            if (tableType != TableType.JsonColumn) {
                return Task.FromResult(new SQLResult());
            }
            var connection = (SyncConnection)syncConnection;
            var columnNames = GetColumnNames(connection, name);
            foreach (var column in columns) {
                if (column == keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                var result = AddVirtualColumn(connection, name, column);
                if (result.Failed) {
                    return Task.FromResult(result);
                }
            }
            return Task.FromResult(new SQLResult());
        }
        
        public Task<SQLResult> AddColumns (ISyncConnection syncConnection) {
            if (tableType != TableType.Relational) {
                return Task.FromResult<SQLResult>(default);
            }
            var connection  = (SyncConnection)syncConnection;
            var columnNames = GetColumnNames(connection, name);
            foreach (var column in columns) {
                if (columnNames.Contains(column.name)) {
                    continue;
                }
                var type    = ConvertContext.GetSqlType(column);
                var sql     = $"ALTER TABLE {name} ADD `{column.name}` {type};";
                var result  = Execute(connection, sql);
                if (result.Failed) {
                    return Task.FromResult(result);
                }
            }
            return Task.FromResult(new SQLResult());
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            if (synchronous) {
                return CreateEntities(command, syncContext);
            }
            return await Task.Run(() => CreateEntities(command, syncContext)).ConfigureAwait(false);
        }
        
        public override CreateEntitiesResult CreateEntities(CreateEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            lock (connection.writeLock) {
                using var scope = connection.BeginTransaction(out var error);
                if (error != null) {
                    return new CreateEntitiesResult { Error = error };
                }
                var sql = new StringBuilder();
                if (tableType == TableType.Relational) {
                    sql.Append($"INSERT INTO {name} VALUES(");
                    for (int n = 0; n < tableInfo.columns.Length; n++) sql.Append("?,");
                    sql.Length--;
                    sql.Append(')');
                } else {
                    sql.Append($@"INSERT INTO {name} VALUES(?,?)");
                }
                using var stmt = Prepare(connection, sql.ToString(), out error);
                if (error != null) {
                    return new CreateEntitiesResult { Error = error };
                }
                if (tableType == TableType.Relational) {
                    if (!AppendColumnValues(connection, stmt.instance, command.entities, tableInfo, syncContext, out error)) {
                        return new CreateEntitiesResult { Error = error };
                    }
                } else {
                    if (!AppendValues(connection, stmt.instance, command.entities, out error)) {
                        return new CreateEntitiesResult { Error = error };
                    }
                }
                if (!scope.EndTransaction("COMMIT TRANSACTION", out error)) {
                    return new CreateEntitiesResult { Error = error };
                }
                return new CreateEntitiesResult();
            }
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            if (synchronous) {
                return UpsertEntities(command, syncContext);
            }
            return await Task.Run(() => UpsertEntities(command, syncContext)).ConfigureAwait(false);
        }

        public override UpsertEntitiesResult UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            lock (connection.writeLock) {
                using var scope = connection.BeginTransaction(out var error);
                if (error != null) {
                    return new UpsertEntitiesResult { Error = error };
                }
                var sql = new StringBuilder();
                if (tableType == TableType.Relational) {
                    sql.Append($"INSERT INTO {name} (");
                    SQLTable.AppendColumnNames(sql, tableInfo);
                    sql.Append(") VALUES(");                   
                    for (int n = 0; n < tableInfo.columns.Length; n++) sql.Append("?,");
                    sql.Length--;
                    sql.Append($") ON CONFLICT([{keyColumn.name}]) DO UPDATE SET ");
                    foreach (var column in tableInfo.columns) {
                        sql.Append($"[{column.name}]=excluded.[{column.name}], ");
                    }
                    sql.Length -= 2;
                } else {
                    sql.Append($"INSERT INTO {name} ({ID}, {DATA}) VALUES(?,?) ON CONFLICT({ID}) DO UPDATE SET {DATA}=excluded.{DATA}");
                }
                using var stmt = Prepare(connection, sql.ToString(), out error);
                if (error != null) {
                    return new UpsertEntitiesResult { Error = error };
                }
                if (tableType == TableType.Relational) {
                    if (!AppendColumnValues(connection, stmt.instance, command.entities, tableInfo, syncContext, out error)) {
                        return new UpsertEntitiesResult { Error = error };
                    }
                } else {
                    if (!AppendValues(connection, stmt.instance, command.entities, out error)) {
                        return new UpsertEntitiesResult { Error = error };
                    }
                }
                if (!scope.EndTransaction("COMMIT TRANSACTION", out error)) {
                    return new UpsertEntitiesResult { Error = error };
                }
                return new UpsertEntitiesResult();
            }
        }
        
        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            if (synchronous) {
                return ReadEntities(command, syncContext);
            }
            return await Task.Run(() => ReadEntities(command, syncContext)).ConfigureAwait(false);
        }

        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            var sql = new StringBuilder();
            if (tableType == TableType.Relational) {
                sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, tableInfo);
                sql.Append($" FROM {name} WHERE {tableInfo.keyColumn.name} in (?)");
            } else {
                sql.Append($"SELECT {ID}, {DATA} FROM {name} WHERE {ID} in (?)");    
            }
            using var stmt = Prepare(connection, sql.ToString(), out var error);
            if (error != null) {
                return new ReadEntitiesResult { Error = error };
            }
            List<EntityValue> values;
            var buffer = syncContext.MemoryBuffer;
            if (tableType == TableType.Relational) {
                using var pooled = syncContext.SQL2Json.Get();
                var mapper  = new SQLiteSQL2Json(pooled.instance, connection, stmt.instance, tableInfo);
                if (!mapper.ReadEntities(command.ids, buffer, out error)) {
                    return new ReadEntitiesResult { Error = error };
                }
                values = pooled.instance.result;
            } else {
                values = new List<EntityValue>(); // TODO - OPTIMIZE
                if (!ReadById(connection, stmt.instance, command.ids, values, buffer, out error)) {
                    return new ReadEntitiesResult { Error = error };
                }
            }
            return new ReadEntitiesResult { entities = new Entities(values) };
        }
        
        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            if (synchronous) {
                return QueryEntities(command, syncContext);
            }
            return await Task.Run(() => QueryEntities(command, syncContext)).ConfigureAwait(false);
        }
        
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            sqlite3_stmt    stmt;
            QueryEnumerator enumerator = null;
            var             maxCount   = command.maxCount;
            string sql;
            if (command.cursor != null) {
                if (!FindCursor(command.cursor, syncContext, out enumerator, out var error)) {
                    return new QueryEntitiesResult { Error = error };
                }
                var queryEnumerator = (SQLiteQueryEnumerator)enumerator;
                stmt    = queryEnumerator.stmt;
                sql     = queryEnumerator.sql;
            } else {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLiteFilter(tableType)}";
                var limit   = command.limit == null ? "" : $" LIMIT {command.limit}";
                var sqlSb   = new StringBuilder();
                if (tableType == TableType.Relational) {
                    sqlSb.Append("SELECT ");
                    SQLTable.AppendColumnNames(sqlSb, tableInfo);
                    sqlSb.Append($" FROM {name}{where}{limit};");
                } else {
                    sqlSb.Append($"SELECT {ID}, {DATA} FROM {name}{where}{limit};");    
                }
                sql = sqlSb.ToString();
                stmt = Prepare(connection, sql, out var error).instance;
                if (error != null) {
                    return new QueryEntitiesResult { Error = error, sql = sql };
                }
            }
            var buffer = syncContext.MemoryBuffer;
            List<EntityValue> values;
            if (tableType == TableType.Relational) {
                using var pooled = syncContext.SQL2Json.Get();
                var mapper  = new SQLiteSQL2Json(pooled.instance, connection, stmt, tableInfo);
                if (!mapper.ReadValues(maxCount, buffer, out var readError)) {
                    return new QueryEntitiesResult { Error = readError, sql = sql };
                }
                values =  pooled.instance.result;
            } else {
                values = new List<EntityValue>();   // TODO remove
                if (!ReadValues(stmt, maxCount, values, buffer, out var readError)) {
                    return new QueryEntitiesResult { Error = readError, sql = sql };
                }
            }
            var result = new QueryEntitiesResult { entities = new Entities(values), sql = sql };
            if (maxCount != null) {
                if (values.Count == maxCount) {
                    enumerator ??= new SQLiteQueryEnumerator(stmt, sql);
                    result.cursor = StoreCursor(enumerator, syncContext.User);
                } else {
                    RemoveCursor(enumerator);
                }
            }
            return result;
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            if (synchronous) {
                return AggregateEntities(command, syncContext);
            }
            return await Task.Run(() => AggregateEntities(command, syncContext)).ConfigureAwait(false);
        }
        
        public override AggregateEntitiesResult AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLiteFilter(tableType)}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                using var stmt = Prepare(connection, sql, out var error);
                if (error != null) {
                    return new AggregateEntitiesResult { Error = error };
                }
                var rc      = raw.sqlite3_step(stmt.instance);
                if (rc != raw.SQLITE_ROW) {
                    var msg = GetErrorMsg("step failed.", connection.sqliteDB, rc);
                    return new AggregateEntitiesResult { Error = new TaskExecuteError(TaskErrorType.DatabaseError, msg) };
                }
                var count   = raw.sqlite3_column_int64(stmt.instance, 0);
                return new AggregateEntitiesResult { value = count };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            if (synchronous) {
                return DeleteEntities(command, syncContext);
            }
            return await Task.Run(() => DeleteEntities(command, syncContext)).ConfigureAwait(false);
        }
        
        public override DeleteEntitiesResult DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                if (!Exec(connection, sql, out var error)) {
                    return new DeleteEntitiesResult { Error = error };    
                }
                return new DeleteEntitiesResult();
            }
            lock (connection.writeLock) {
                using var scope = connection.BeginTransaction(out var error);
                if (error != null) {
                    return new DeleteEntitiesResult { Error = error };
                }
                var id = tableType == TableType.Relational ? keyColumn.name : ID;
                var sql = $"DELETE from {name} WHERE [{id}] in (?)";
                using var stmt = Prepare(connection, sql, out error);
                if (error != null) {
                    return new DeleteEntitiesResult { Error = error };
                }
                if (!AppendKeys(connection, stmt.instance, command.ids, out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                if (!scope.EndTransaction("COMMIT TRANSACTION", out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                return new DeleteEntitiesResult();
            }
        }
    }
}

#endif