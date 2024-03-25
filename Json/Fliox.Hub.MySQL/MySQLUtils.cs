// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal static class MySQLUtils
    {
        internal static string GetErrMsg(MySqlException exception) {
            return exception.Message;
        }
        
        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return SQLResult.Success(value); 
                    // Console.WriteLine($"MySQL version: {value}");
                }
                return default;
            }
            catch (MySqlException e) {
                var msg = GetErrMsg(e);
                return SQLResult.CreateError(msg);
            }
        }
        
        internal static async Task<SQLResult> AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column, MySQLProvider provider)
        {
            var type    = ConvertContext.GetSqlType(column, provider);
            var colName = column.name; 
            var asStr   = GetColumnAs(column, provider);
            
            switch (provider) {
                case MySQLProvider.MARIA_DB: {
var sql =
$@"ALTER TABLE {table}
ADD COLUMN IF NOT EXISTS `{colName}` {type}
GENERATED ALWAYS AS {asStr} VIRTUAL;";
                    return await Execute(connection, sql).ConfigureAwait(false);
                }
                case MySQLProvider.MY_SQL: {
var sql = $@"ALTER TABLE {table}
ADD COLUMN `{colName}` {type}
GENERATED ALWAYS AS {asStr} VIRTUAL;";
                    return await Execute(connection, sql).ConfigureAwait(false);
                }
            }
            throw new InvalidOperationException($"invalid provider {provider}");
        }
        
        private static string GetColumnAs(ColumnInfo column, MySQLProvider provider) {
            var colName = column.name;
            var asStr   = $"(JSON_VALUE({DATA}, '$.{colName}'))";
            switch (column.type) {
                case ColumnType.JsonValue:
                    if (provider == MySQLProvider.MY_SQL) {
                        return $"(JSON_VALUE({DATA}, '$.{colName}' RETURNING JSON))";
                    }
                    return $"(JSON_QUERY({DATA}, '$.{colName}'))";
                case ColumnType.Object:
                    if (provider == MySQLProvider.MY_SQL) {
                        return $"(!ISNULL(JSON_VALUE({DATA}, '$.{colName}')))";    
                    }
                    return $"(!ISNULL(JSON_QUERY({DATA}, '$.{colName}')))";
                case ColumnType.DateTime:
                    return $"(STR_TO_DATE(TRIM(TRAILING 'Z' FROM {asStr}), '%Y-%m-%dT%H:%i:%s.%f'))";
                case ColumnType.Boolean:
                    if (provider == MySQLProvider.MY_SQL) {
                        return $"(case when {asStr} = 'true' then 1 when {asStr} = 'false' then 0 end)";
                    }
                    return asStr;
                default:
                    return asStr;
            }
        }
        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new MySqlConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"CREATE DATABASE IF NOT EXISTS {database}";
            using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        
        private static string GetDbmsConnectionString(string connectionString, out string database) {
            var builder  = new MySqlConnectionStringBuilder(connectionString);
            database = builder.Database;
            builder.Remove("Database");
            return builder.ToString();
        }
    }
}

#endif