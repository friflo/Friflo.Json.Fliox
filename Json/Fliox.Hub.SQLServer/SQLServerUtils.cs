// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using System.Data.SqlClient;
using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal static partial class SQLServerUtils
    {
        internal static string GetErrMsg(SqlException exception) {
            var error = exception.Errors[0]; // always present
            return error.Message;
        }

        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return SQLResult.Success(value); 
                }
                return default;
            }
            catch (SqlException e) {
                return SQLResult.CreateError(e);
            }
        }
        
        internal static async Task<SQLResult> AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column) {
            var asStr = GetColumnAs(column);
            var sql =
$@"ALTER TABLE {table}
ADD ""{column.name}""
AS ({asStr});";
            return await Execute(connection, sql).ConfigureAwait(false);
        }
        
        private static string GetColumnAs(ColumnInfo column) {
            switch (column.type) {
                case ColumnType.Object:
                    return $"IIF(JSON_QUERY({DATA}, '$.{column.name}') is null, 0, 1)";
                default:
                    var type  = ConvertContext.GetSqlType(column);
                    return $"CAST(JSON_VALUE({DATA}, '$.{column.name}') AS {type})";
            }
        }
        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new SqlConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{database}') CREATE DATABASE {database}";
            using var cmd = new SqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        
        private static string GetDbmsConnectionString(string connectionString, out string database) {
            var builder  = new SqlConnectionStringBuilder(connectionString);
            database = builder.InitialCatalog;
            builder.Remove("Database");
            return builder.ToString();
        }
        
        public static string QueryEntities(QueryEntities command, string table, string filter, TableInfo tableInfo) {
            var tableType   = tableInfo.tableType;
            if (command.maxCount == null) {
                var top = command.limit == null ? "" : $" TOP {command.limit}";
                if (tableType == TableType.Relational) {
                    var sql = new StringBuilder();
                    sql.Append($"SELECT{top} ");
                    SQLTable.AppendColumnNames(sql, tableInfo);
                    sql.Append($" FROM {table} WHERE {filter}");
                    return sql.ToString();
                }
                return $"SELECT{top} {ID}, {DATA} FROM {table} WHERE {filter}";
            } else {
                var id = tableType == TableType.Relational ? tableInfo.keyColumn.name : ID;
                var cursorStart = command.cursor == null ? "" : $"{id} < '{command.cursor}' AND ";
                var cursorDesc  = command.maxCount == null ? "" : $" ORDER BY {id} DESC";
                var sql = new StringBuilder();
                sql.Append("SELECT ");
                if (tableType == TableType.Relational) {
                    SQLTable.AppendColumnNames(sql, tableInfo);
                } else {
                    sql.Append($"{ID}, {DATA}");
                    // return $"SELECT {ID}, {DATA} FROM {table} WHERE {cursorStart}{filter}{cursorDesc} OFFSET 0 ROWS FETCH FIRST {command.maxCount} ROWS ONLY";
                }
                sql.Append($" FROM {table} WHERE {cursorStart}{filter}{cursorDesc} OFFSET 0 ROWS FETCH FIRST {command.maxCount} ROWS ONLY");
                return sql.ToString();
            }
        }
        
        // --- create / upsert using DataTable in SQL statement
        internal static async Task CreateEntitiesCmdAsync (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = $"INSERT INTO {table} ({ID},{DATA}) select {ID}, {DATA} from @rows;";
            var p = AddRows(entities);
            await connection.ExecuteNonQueryAsync(sql, p).ConfigureAwait(false);
        }
        
        internal static async Task UpsertEntitiesCmdAsync (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = $@"
MERGE {table} AS target
USING @rows as source
ON source.{ID} = target.{ID}
WHEN MATCHED THEN
    UPDATE SET target.{DATA} = source.{DATA}
WHEN NOT MATCHED THEN
    INSERT ({ID}, {DATA})
    VALUES ({ID}, {DATA});";
            var p = AddRows(entities);
            await connection.ExecuteNonQueryAsync(sql, p).ConfigureAwait(false);
        }

        private static SqlParameter AddRows(List<JsonEntity> entities) {
            var p = new SqlParameter("@rows", SqlDbType.Structured);
            p.Value = SQLUtils.ToDataTable(entities);
            // DataTable requires registering UDT (User defined type) in database before execution:
            // CREATE TYPE KeyValueType AS TABLE({ID} varchar(128), {DATA} varchar(max));
            p.TypeName = "KeyValueType";
            return p;
        }
        
        internal static async Task CreateRelationalValues (
            SyncConnection      connection,
            List<JsonEntity>    entities,
            TableInfo           tableInfo,
            SyncContext         syncContext)
        {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {tableInfo.container} (");
            SQLTable.AppendColumnNames(sql, tableInfo);
            sql.Append(")\nVALUES\n");
            using var pooled    = syncContext.Json2SQL.Get();
            var writer          = new Json2SQLWriter(pooled.instance, sql, SQLEscape.BackSlash);
            pooled.instance.AppendColumnValues(writer, entities, tableInfo);
            await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
        }
        
        internal static async Task UpsertRelationalValues (
            SyncConnection      connection,
            List<JsonEntity>    entities,
            TableInfo           tableInfo,
            SyncContext         syncContext)
        {
            var sql = new StringBuilder();
            var id  = tableInfo.tableType == TableType.JsonColumn ? ID : tableInfo.keyColumn.name;
            sql.Append(
$@"MERGE {tableInfo.container} AS t USING (
VALUES
");
            using var pooled    = syncContext.Json2SQL.Get();
            var writer          = new Json2SQLWriter (pooled.instance, sql, SQLEscape.PrefixN);
            pooled.instance.AppendColumnValues(writer, entities, tableInfo);
            sql.Append($@") AS s (");
            SQLTable.AppendColumnNames(sql, tableInfo);
            sql.Append($@")
ON s.{id} = t.{id}
WHEN MATCHED THEN
    UPDATE SET");
            foreach (var column in tableInfo.columns) {
                sql.Append(" t.[");
                sql.Append(column.name);
                sql.Append("]=s.[");
                sql.Append(column.name);
                sql.Append("],");
            }
            sql.Length -= 1;
            sql.Append($@"
WHEN NOT MATCHED THEN
    INSERT (");
            SQLTable.AppendColumnNames(sql, tableInfo);
            sql.Append(")\n    VALUES(");
            SQLTable.AppendColumnNames(sql, tableInfo);
            sql.Append(");");
            await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
        }
        
        internal static async Task DeleteEntitiesCmdAsync (SyncConnection connection, List<JsonKey> ids, string table) {
            var dataTable = new DataTable();
            dataTable.Columns.Add(ID, typeof(string));
            foreach(var id in ids) {
                dataTable.Rows.Add(id.AsString());
            }
            var sql     = $"DELETE FROM  {table} WHERE {ID} in (SELECT {ID} FROM @ids);";
            var p       = new SqlParameter("@ids", SqlDbType.Structured);
            p.Value     = dataTable;
            p.TypeName  = "KeyType";
            await connection.ExecuteNonQueryAsync(sql, p).ConfigureAwait(false);
        }
        
        internal static async Task<SqlDataReader> ReadEntitiesCmd (SyncConnection connection, List<JsonKey> ids, string table) {
            var dataTable = new DataTable();
            dataTable.Columns.Add(ID, typeof(string));
            foreach(var id in ids) {
                dataTable.Rows.Add(id.AsString());
            }
            var sql     = $"SELECT {ID}, {DATA} FROM {table} WHERE {ID} in (SELECT {ID} FROM @ids);";
            var p       = new SqlParameter("@ids", SqlDbType.Structured);
            p.Value     = dataTable;
            p.TypeName  = "KeyType";
            return await connection.ExecuteReaderAsync(sql, p).ConfigureAwait(false);
        }
    }
}

#endif