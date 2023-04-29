// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using MySqlConnector;

namespace Friflo.Json.Fliox.Hub.MySQL
{
    public static class MySQLUtils
    {
        public static async Task OpenOrCreateDatabase(MySqlConnection connection, string db) {
            var sql = $"CREATE DATABASE IF NOT EXISTS {db}";
            using var command = new MySqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var value = reader.GetValue(0);
                // Console.WriteLine($"MySQL version: {value}");
            }
        }
        
        internal static async Task<MySQLResult> Execute(MySqlConnection connection, string sql) {
            using var command = new MySqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var value = reader.GetValue(0);
                return new MySQLResult(value); 
                // Console.WriteLine($"MySQL version: {value}");
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
    
    internal readonly struct MySQLResult
    {
        internal readonly object           value;
        internal readonly TaskExecuteError error;
        
        internal MySQLResult(object value) {
            this.value  = value;
            error       = null;
        }
        
        internal MySQLResult(TaskExecuteError error) {
            value       = null;
            this.error  = error;
        }

    }
}

#endif