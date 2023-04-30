// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Npgsql;

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public static class PostgreSQLUtils
    {
        internal static async Task<string> GetVersion(NpgsqlConnection connection) {
            
            var result  = await Execute(connection, "select version()").ConfigureAwait(false);
            var version = result.error != null ? "" : result.value;
            return  $"PostgreSQL {version}";
        }
        
        public static async Task OpenOrCreateDatabase(NpgsqlConnection connection, string db) {
            var sql = $"CREATE DATABASE IF NOT EXISTS {db}";
            using var command = new NpgsqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var value = reader.GetValue(0);
            }
        }
        
        internal static async Task<SQLResult> Execute(NpgsqlConnection connection, string sql) {
            using var command = new NpgsqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var value = reader.GetValue(0);
                return new SQLResult(value); 
            }
            return default;
        }
        
        internal static void AppendValues(StringBuilder sb, List<JsonEntity> entities) {
            var isFirst = true;
            foreach (var entity in entities)
            {
                if (isFirst) {
                    isFirst = false;   
                } else {
                    sb.Append(',');
                }
                sb.Append("('");
                entity.key.AppendTo(sb);
                sb.Append("','");
                sb.Append(entity.value.AsString());
                sb.Append("')");
            }
        }
        
        internal static void AppendKeys(StringBuilder sb, List<JsonKey> keys) {
            var isFirst = true;
            sb.Append('(');
            foreach (var key in keys)
            {
                if (isFirst) {
                    isFirst = false;   
                } else {
                    sb.Append(',');
                }
                sb.Append('\'');
                key.AppendTo(sb);
                sb.Append('\'');
            }
            sb.Append(')');
        }
    }
}

#endif