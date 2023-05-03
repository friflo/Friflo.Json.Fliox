// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using MySqlConnector;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.MySQL
{
    public static class MySQLUtils
    {
        internal static async Task<string> GetVersion(MySqlConnection connection) {
            var result  = await Execute(connection, "select version()").ConfigureAwait(false);
            var version = result.error != null ? "" : result.value;
            return  $"MySQL {version}";
        }
        
        public static async Task OpenOrCreateDatabase(MySqlConnection connection, string db) {
            var sql = $"CREATE DATABASE IF NOT EXISTS {db}";
            await Execute(connection, sql).ConfigureAwait(false);
        }
        
        internal static async Task<SQLResult> Execute(MySqlConnection connection, string sql) {
            try {
                using var command = new MySqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return new SQLResult(value); 
                    // Console.WriteLine($"MySQL version: {value}");
                }
                return default;
            }
            catch (MySqlException e) {
                return new SQLResult(e.Message);
            }
        }
    }
}

#endif