// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using System.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal static partial class SQLServerUtils
    {
        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return new SQLResult(value); 
                }
                return default;
            }
            catch (SqlException e) {
                return new SQLResult(e.Message);
            }
        }
        
        internal static async Task AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column) {
            var type = ConvertContext.GetSqlType(column.type);
            var sql =
$@"ALTER TABLE {table}
ADD ""{column.name}""
AS CAST(JSON_VALUE({DATA}, '$.{column.name}') AS {type});";
            await Execute(connection, sql).ConfigureAwait(false);
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
        
        public static string QueryEntities(QueryEntities command, string table, string filter) {
            if (command.maxCount == null) {
                var top = command.limit == null ? "" : $" TOP {command.limit}";
                return $"SELECT{top} {ID}, {DATA} FROM {table} WHERE {filter}";
            }
            var cursorStart = command.cursor == null ? "" : $"{ID} < '{command.cursor}' AND ";
            var cursorDesc  = command.maxCount == null ? "" : $" ORDER BY {ID} DESC";
            return $"SELECT {ID}, {DATA} FROM {table} WHERE {cursorStart}{filter}{cursorDesc} OFFSET 0 ROWS FETCH FIRST {command.maxCount} ROWS ONLY";
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