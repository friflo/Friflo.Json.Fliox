// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Npgsql;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    internal static class PostgreSQLUtils
    {
        internal static async Task<SQLResult> ExecuteAsync(SyncConnection connection, string sql) {
            try {
                using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return new SQLResult(value); 
                }
                return default;
            }
            catch (NpgsqlException e) {
                return new SQLResult(e);
            }
        }
        
        internal static async Task AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column) {
            var type    = ConvertContext.GetSqlType(column.type);
            var asStr   = GetColumnAs(column);
            var sql =
$@"ALTER TABLE {table}
ADD COLUMN IF NOT EXISTS ""{column.name}"" {type} NULL
GENERATED ALWAYS AS (({asStr})::{type}) STORED;";
            await ExecuteAsync(connection, sql).ConfigureAwait(false);
        }
        
        private static string GetColumnAs(ColumnInfo column) {
            var asStr   = ConvertContext.ConvertPath(DATA, column.name, 0);
            switch (column.type) {
                case ColumnType.Object:
                    return $"({asStr} is not null)";
                default:
                    return asStr;
            }
        }
        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new NpgsqlConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"CREATE DATABASE {database}";
            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        
        private static string GetDbmsConnectionString(string connectionString, out string database) {
            var builder  = new NpgsqlConnectionStringBuilder(connectionString);
            database = builder.Database;
            builder.Remove("Database");
            return builder.ToString();
        }
    }
}

#endif