// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Schema.Definition;
using Npgsql;
using static Friflo.Json.Fliox.Hub.Host.Utils.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public static class PostgreSQLUtils
    {
        internal static NpgsqlCommand Command (string sql, SyncConnection connection) {
            return new NpgsqlCommand(sql, connection.instance as NpgsqlConnection);
        }
        
        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var command = new NpgsqlCommand(sql, connection.instance as NpgsqlConnection);
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
        
        internal static string GetSqlType(StandardTypeId typeId) {
            switch (typeId) {
                case StandardTypeId.Uint8:      return "smallint";
                case StandardTypeId.Int16:      return "smallint";
                case StandardTypeId.Int32:      return "integer";
                case StandardTypeId.Int64:      return "bigint";
                case StandardTypeId.Float:      return "float";
                case StandardTypeId.Double:     return "double precision";
                case StandardTypeId.Boolean:    return "boolean";
                case StandardTypeId.DateTime:
                case StandardTypeId.Guid:
                case StandardTypeId.BigInteger:
                case StandardTypeId.String:
                case StandardTypeId.Enum:       return "text";
            }
            throw new NotSupportedException($"column type: {typeId}");
        }
        
        internal static async Task AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column) {
            var type = GetSqlType(column.typeId);
            var sql =
$@"ALTER TABLE {table}
ADD COLUMN IF NOT EXISTS ""{column.name}"" {type} NULL
GENERATED ALWAYS AS (({DATA} ->> '{column.name}')::{type}) STORED;";
            await Execute(connection, sql);
        }
        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new NpgsqlConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"CREATE DATABASE {database}";
            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
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