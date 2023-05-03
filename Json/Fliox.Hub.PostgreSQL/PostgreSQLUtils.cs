// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Npgsql;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public static class PostgreSQLUtils
    {
        internal static async Task<string> GetVersion(NpgsqlConnection connection) {
            
            var result  = await Execute(connection, "select version()").ConfigureAwait(false);
            var version = !result.Success ? "" : result.value;
            return  $"PostgreSQL {version}";
        }
        
        public static async Task OpenOrCreateDatabase(NpgsqlConnection connection, string db) {
            var sql = $"CREATE DATABASE IF NOT EXISTS {db}";
            await Execute(connection, sql).ConfigureAwait(false);
        }
        
        internal static async Task<SQLResult> Execute(NpgsqlConnection connection, string sql) {
            try {
                using var command = new NpgsqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return new SQLResult(value); 
                }
                return default;
            }
            catch (PostgresException e) {
                return new SQLResult(e.MessageText);
            }
        }
    }
}

#endif