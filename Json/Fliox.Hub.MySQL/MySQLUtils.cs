// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Schema.Definition;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.Host.Utils.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.MySQL
{
    public static class MySQLUtils
    {
        internal static MySqlCommand Command (string sql, SyncConnection connection) {
            return new MySqlCommand(sql, connection.instance as MySqlConnection);
        }
        
        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var command = new MySqlCommand(sql, connection.instance as MySqlConnection);
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
        
        private static string GetSqlType(StandardTypeId typeId, MySQLProvider provider) {
            switch (typeId) {
                case StandardTypeId.Uint8:      return "tinyint";
                case StandardTypeId.Int16:      return "smallint";
                case StandardTypeId.Int32:      return "integer";
                case StandardTypeId.Int64:      return "bigint";
                case StandardTypeId.Float:      return "float";
                case StandardTypeId.Double:     return "double precision";
                case StandardTypeId.Boolean:    return "text";
                case StandardTypeId.DateTime:
                case StandardTypeId.Guid:
                case StandardTypeId.BigInteger:
                case StandardTypeId.String:
                case StandardTypeId.Enum:       return "text";
            }
            throw new NotSupportedException($"column type: {typeId}");
        }
        
        internal static async Task AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column, MySQLProvider provider) {
            var type = GetSqlType(column.typeId, provider);
            var colName = column.name; 
            switch (provider) {
                case MySQLProvider.MARIA_DB: {
var sql =
$@"ALTER TABLE {table}
ADD COLUMN IF NOT EXISTS {colName} {type}
GENERATED ALWAYS AS (JSON_VALUE({DATA}, '$.{colName}')) VIRTUAL;";
                    await Execute(connection, sql);
                    return;
                }
                case MySQLProvider.MY_SQL: {
var sql =    
$@"SELECT COUNT(*)
FROM `INFORMATION_SCHEMA`.`COLUMNS`
WHERE `TABLE_NAME`= '{table}' AND `COLUMN_NAME` = '{colName}';";
                    var result = await Execute(connection, sql);
                    if (result.Failed) {
                        return;
                    }
                    if ((long)result.value != 0)
                        return;
                    /*var jsonValue = column.typeId == StandardTypeId.Boolean ?
                        $"(IF(JSON_VALUE({DATA}, '$.{colName}')=true, 1,0))" :
                        $"(JSON_VALUE({DATA}, '$.{colName}'))";*/
sql = $@"alter table {table}
add column {colName} {type}
as (JSON_VALUE({DATA}, '$.{colName}')) VIRTUAL;";
                    await Execute(connection, sql);
                    return;
                }
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