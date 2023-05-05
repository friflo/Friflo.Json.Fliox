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
            var sql = @$"
IF TYPE_ID(N'KeyValueType') IS NULL CREATE TYPE KeyValueType AS TABLE(id varchar(128), data varchar(max));
INSERT INTO {table} (id,data) select id, data from @rows;";
            using var cmd = Command(sql, connection);
            AddRows(cmd, entities);
            return cmd;
        }
        
        internal static DbCommand UpsertEntitiesCmd (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = $@"
IF TYPE_ID(N'KeyValueType') IS NULL CREATE TYPE KeyValueType AS TABLE(id varchar(128), data varchar(max));
MERGE {table} AS target
USING @rows as source
ON source.id = target.id
WHEN MATCHED THEN
    UPDATE SET target.data = source.data
WHEN NOT MATCHED THEN
    INSERT (id, data)
    VALUES (id, data);";
            using var cmd = Command(sql, connection);
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
    }
}

#endif