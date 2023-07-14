// Copyright (c) Ullrich Praetz. All rights reserved.
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
                return Task.FromResult(SQLiteUtils.Execute(connection, sql));
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
            var result = SQLiteUtils.Execute(connection, sb.ToString());
            return Task.FromResult(result);
        }
        
        public Task<SQLResult> AddVirtualColumns(ISyncConnection syncConnection) {
            if (tableType != TableType.JsonColumn) {
                return Task.FromResult(new SQLResult());
            }
            var connection = (SyncConnection)syncConnection;
            var columnNames = SQLiteUtils.GetColumnNames(connection, name);
            foreach (var column in columns) {
                if (column == keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                var result = SQLiteUtils.AddVirtualColumn(connection, name, column);
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
            var columnNames = SQLiteUtils.GetColumnNames(connection, name);
            foreach (var column in columns) {
                if (columnNames.Contains(column.name)) {
                    continue;
                }
                var type    = ConvertContext.GetSqlType(column);
                var sql     = $"ALTER TABLE {name} ADD `{column.name}` {type};";
                var result  = SQLiteUtils.Execute(connection, sql);
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
                if (!SQLiteUtils.Prepare(connection, sql.ToString(), out var stmt, out error)) {
                    return new CreateEntitiesResult { Error = error };
                }
                if (tableType == TableType.Relational) {
                    if (!SQLiteUtils.AppendColumnValues(stmt, command.entities, tableInfo, syncContext, out error)) {
                        return new CreateEntitiesResult { Error = error };
                    }
                } else {
                    if (!SQLiteUtils.AppendValues(stmt, command.entities, out error)) {
                        return new CreateEntitiesResult { Error = error };
                    }
                }
                raw.sqlite3_finalize(stmt);
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
                    sql.Append($"INSERT INTO {name} VALUES(");
                    for (int n = 0; n < tableInfo.columns.Length; n++) sql.Append("?,");
                    sql.Length--;
                    sql.Append($") ON CONFLICT([{keyColumn.name}]) DO UPDATE SET ");
                    foreach (var column in tableInfo.columns) {
                        sql.Append($"[{column.name}]=excluded.[{column.name}], ");
                    }
                    sql.Length -= 2;
                } else {
                    sql.Append($"INSERT INTO {name} VALUES(?,?) ON CONFLICT({ID}) DO UPDATE SET {DATA}=excluded.{DATA}");
                }
                if (!SQLiteUtils.Prepare(connection, sql.ToString(), out var stmt, out error)) {
                    return new UpsertEntitiesResult { Error = error };
                }
                if (tableType == TableType.Relational) {
                    if (!SQLiteUtils.AppendColumnValues(stmt, command.entities, tableInfo, syncContext, out error)) {
                        return new UpsertEntitiesResult { Error = error };
                    }
                } else {
                    if (!SQLiteUtils.AppendValues(stmt, command.entities, out error)) {
                        return new UpsertEntitiesResult { Error = error };
                    }
                }
                raw.sqlite3_finalize(stmt);
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
                sql.Append($" FROM {name} WHERE {tableInfo.keyColumn.name} in\n");
            } else {
                sql.Append($"SELECT {ID}, {DATA} FROM {name} WHERE {ID} in (?)");    
            }
            if (!SQLiteUtils.Prepare(connection, sql.ToString(), out var stmt, out var error)) {
                return new ReadEntitiesResult { Error = error };
            }
            var values = new List<EntityValue>(); // TODO remove
            if (tableType == TableType.Relational) {
                using var pooled = syncContext.SQL2Json.Get();
                var mapper  = new SQLiteSQL2Json(pooled.instance, stmt, tableInfo);
                values      = mapper.ReadEntities(tableInfo);
            } else {
                if (!SQLiteUtils.ReadById(stmt, command.ids, values, syncContext.MemoryBuffer, out error)) {
                    return new ReadEntitiesResult { Error = error };
                }
            }
            return new ReadEntitiesResult { entities = values.ToArray() };
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
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLiteFilter()}";
                var limit   = command.limit == null ? "" : $" LIMIT {command.limit}";
                var sqlSb   = new StringBuilder();
                if (tableType == TableType.Relational) {
                    sqlSb.Append("SELECT ");
                    SQLTable.AppendColumnNames(sqlSb, tableInfo);
                    sqlSb.Append($" FROM {name}{where}{limit}");
                } else {
                    sqlSb.Append($"SELECT {ID}, {DATA} FROM {name}{where}{limit}");    
                }
                sql = sqlSb.ToString();
                if (!SQLiteUtils.Prepare(connection, sql, out stmt, out var error)) {
                    return new QueryEntitiesResult { Error = error, sql = sql };
                }
            }
            var values = new List<EntityValue>();   // TODO remove
            if (tableType == TableType.Relational) {
                using var pooled = syncContext.SQL2Json.Get();
                var mapper  = new SQLiteSQL2Json(pooled.instance, stmt, tableInfo);
                if (!mapper.ReadValues(maxCount, values, syncContext.MemoryBuffer, out var readError)) {
                    return new QueryEntitiesResult { Error = readError, sql = sql };
                }
            } else {
                if (!SQLiteUtils.ReadValues(stmt, maxCount, values, syncContext.MemoryBuffer, out var readError)) {
                    return new QueryEntitiesResult { Error = readError, sql = sql };
                }
            }
            var result = new QueryEntitiesResult { entities = values.ToArray(), sql = sql };
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
        
        private AggregateEntitiesResult AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLiteFilter()}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                if (!SQLiteUtils.Prepare(connection, sql, out var stmt, out var error)) {
                    return new AggregateEntitiesResult { Error = error };
                }
                var rc      = raw.sqlite3_step(stmt);
                if (rc != raw.SQLITE_ROW) {
                    var msg = $"step failed. sql: {sql}. error: {rc}";
                    return new AggregateEntitiesResult { Error = new TaskExecuteError(TaskErrorType.DatabaseError, msg) };
                }
                var count   = raw.sqlite3_column_int64(stmt, 0);
                raw.sqlite3_finalize(stmt);
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
                if (!SQLiteUtils.Exec(connection, sql, out var error)) {
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
                if (!SQLiteUtils.Prepare(connection, sql, out var stmt, out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                if (!SQLiteUtils.AppendKeys(stmt, command.ids, out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                raw.sqlite3_finalize(stmt);
                if (!scope.EndTransaction("COMMIT TRANSACTION", out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                return new DeleteEntitiesResult();
            }
        }
    }
}

#endif