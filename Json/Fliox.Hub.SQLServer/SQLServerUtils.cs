// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Microsoft.Data.SqlClient;


// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public static class SQLServerUtils
    {
        internal static async Task<string> GetVersion(SqlConnection connection) {
            var result  = await Execute(connection, "select version()").ConfigureAwait(false);
            var version = !result.Success ? "" : result.value;
            return  $"Microsoft SQL Server {version}";
        }
        
        public static async Task OpenOrCreateDatabase(SqlConnection connection, string db) {
            var sql = $"CREATE DATABASE IF NOT EXISTS {db}";
            await Execute(connection, sql).ConfigureAwait(false);
        }
        
        internal static async Task<SQLResult> Execute(SqlConnection connection, string sql) {
            try {
                using var command = new SqlCommand(sql, connection);
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
    }
}

#endif