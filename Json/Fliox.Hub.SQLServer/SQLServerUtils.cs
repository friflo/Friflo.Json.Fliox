// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Microsoft.Data.SqlClient;


// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public static partial class SQLServerUtils
    {
        internal static SqlCommand Command (string sql, SyncConnection connection) {
            return new SqlCommand(sql, connection.instance as SqlConnection);
        }
        
        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var command = new SqlCommand(sql, connection.instance as SqlConnection);
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
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
        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new SqlConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{database}') CREATE DATABASE {database}";
            using var cmd = new SqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
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
                return $"SELECT{top} id, data FROM {table} WHERE {filter}";
            }
            var cursorStart = command.cursor == null ? "" : $"id < '{command.cursor}' AND ";
            var cursorDesc  = command.maxCount == null ? "" : " ORDER BY id DESC";
            return $"SELECT id, data FROM {table} WHERE {cursorStart}{filter}{cursorDesc} OFFSET 0 ROWS FETCH FIRST {command.maxCount} ROWS ONLY";
        }
        
        // --- create / upsert using DataTable in SQL statement
        internal static DbCommand CreateEntitiesCmd (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = $"INSERT INTO {table} (id,data) select id, data from @rows;";
            var cmd = Command(sql, connection);
            AddRows(cmd, entities);
            return cmd;
        }
        
        internal static DbCommand UpsertEntitiesCmd (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = $@"
MERGE {table} AS target
USING @rows as source
ON source.id = target.id
WHEN MATCHED THEN
    UPDATE SET target.data = source.data
WHEN NOT MATCHED THEN
    INSERT (id, data)
    VALUES (id, data);";
            var cmd = Command(sql, connection);
            AddRows(cmd, entities);
            return cmd;
        }

        private static void AddRows(SqlCommand cmd, List<JsonEntity> entities) {
            var p = cmd.Parameters.Add(new SqlParameter("@rows", SqlDbType.Structured));
            p.Value = SQLUtils.ToDataTable(entities);
            // DataTable requires registering UDT (User defined type) in database before execution:
            // CREATE TYPE KeyValueType AS TABLE(id varchar(128), data varchar(max));
            p.TypeName = "KeyValueType";
        }
        
        internal static DbCommand DeleteEntitiesCmd (SyncConnection connection, List<JsonKey> ids, string table) {
            var dataTable = new DataTable();
            dataTable.Columns.Add("id", typeof(string));
            foreach(var id in ids) {
                dataTable.Rows.Add(id.AsString());
            }
            var sql     = $"DELETE FROM  {table} WHERE id in (SELECT id FROM @ids);";
            var cmd     = Command(sql, connection);
            var p       = cmd.Parameters.Add(new SqlParameter("@ids", SqlDbType.Structured));
            p.Value     = dataTable;
            p.TypeName  = "KeyType";
            return cmd;
        }
    }
}

#endif