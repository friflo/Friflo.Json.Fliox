// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Npgsql;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable ConvertTypeCheckPatternToNullCheck
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
                    return SQLResult.Success(value); 
                }
                return default;
            }
            catch (NpgsqlException e) {
                return SQLResult.CreateError(e);
            }
        }
        
        internal static async Task<SQLResult> AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column) {
            var type    = ConvertContext.GetSqlType(column.type);
            var asStr   = GetColumnAs(column);
            var sql =
$@"ALTER TABLE {table}
ADD COLUMN IF NOT EXISTS ""{column.name}"" {type} NULL
GENERATED ALWAYS AS (({asStr})::{type}) STORED;";
            return await ExecuteAsync(connection, sql).ConfigureAwait(false);
        }
        
        private static string GetColumnAs(ColumnInfo column) {
            var asStr = ConvertContext.ConvertPath(DATA, column.name, 0, AsType.Text);
            switch (column.type) {
                case ColumnType.JsonValue:
                    return ConvertContext.ConvertPath(DATA, column.name, 0, AsType.JSON);
                case ColumnType.DateTime:
                    return $"(text2ts{asStr})";
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
        
        // ------ read raw SQL
        internal static async Task<RawSqlResult> ReadRowsAsync(DbDataReader reader) {
            var columns     = GetFieldTypes(reader);
            var data        = new JsonTable();
            var readRawSql  = new ReadRawSql(reader);
            var rowCount    = 0;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                rowCount++;
                AddRow(reader, columns, data, readRawSql);
                data.WriteNewRow();
            }
            return new RawSqlResult(columns, data, rowCount);
        }
        
        internal static RawSqlResult ReadRowsSync(DbDataReader reader) {
            var columns     = GetFieldTypes(reader);
            var data        = new JsonTable();
            var readRawSql  = new ReadRawSql(reader);
            var rowCount    = 0;
            while (reader.Read())
            {
                rowCount++;
                AddRow(reader, columns, data, readRawSql);
                data.WriteNewRow();
            }
            return new RawSqlResult(columns, data, rowCount);
        }
        
        
        
        // ReSharper disable once MemberCanBePrivate.Global
        private static RawSqlColumn[] GetFieldTypes(DbDataReader reader) {
            var count   = reader.FieldCount;
            var columns = new RawSqlColumn[count];
            var schema  = reader.GetColumnSchema();
            for (int n = 0; n < count; n++) {
                var column  = schema[n];
                var type    = column.DataType;
                RawColumnType fieldType = type switch {
                    Type _ when type == typeof(bool)        => RawColumnType.Bool,
                    //
                    Type _ when type == typeof(byte)        => RawColumnType.Uint8,
                    Type _ when type == typeof(sbyte)       => RawColumnType.Int16,
                    Type _ when type == typeof(short)       => RawColumnType.Int16,
                    Type _ when type == typeof(int)         => RawColumnType.Int32,
                    Type _ when type == typeof(long)        => RawColumnType.Int64,
                    //
                    Type _ when type == typeof(string)      => reader.GetDataTypeName(n) == "jsonb" ? RawColumnType.JSON : RawColumnType.String,
                    Type _ when type == typeof(DateTime)    => RawColumnType.DateTime,
                    Type _ when type == typeof(Guid)        => RawColumnType.Guid,
                    //
                    Type _ when type == typeof(double)      => RawColumnType.Double,
                    Type _ when type == typeof(float)       => RawColumnType.Float,
                    //
                    _                                       => RawColumnType.Unknown
                };
                columns[n] = new RawSqlColumn(column.ColumnName, fieldType);
            }
            return columns;
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        private static void AddRow(DbDataReader reader, RawSqlColumn[] columns, JsonTable data, ReadRawSql rawSql) {
            var count   = columns.Length;
            for (int n = 0; n < count; n++) {
                if (reader.IsDBNull(n)) {
                    data.WriteNull();
                    continue;
                }
                var type = columns[n].type;
                switch (type) {
                    case RawColumnType.Bool:        data.WriteBoolean     (reader.GetBoolean  (n));           break;
                    case RawColumnType.Uint8:       data.WriteByte        (reader.GetByte     (n));           break;
                    case RawColumnType.Int16:       data.WriteInt16       (reader.GetInt16    (n));           break;
                    case RawColumnType.Int32:       data.WriteInt32       (reader.GetInt32    (n));           break;
                    case RawColumnType.Int64:       data.WriteInt64       (reader.GetInt64    (n));           break;
                    case RawColumnType.Float:       data.WriteFlt32       (reader.GetFloat    (n));           break;
                    case RawColumnType.Double:      data.WriteFlt64       (reader.GetDouble   (n));           break;
                    case RawColumnType.String:      data.WriteCharString  (rawSql.GetString   (n));           break;
                    case RawColumnType.DateTime:    data.WriteDateTime    (reader.GetDateTime (n));           break;
                    case RawColumnType.Guid:        data.WriteGuid        (reader.GetGuid     (n));           break;
                    case RawColumnType.JSON:        data.WriteCharJSON    (reader.GetString   (n).AsSpan());  break;
                    default:                        data.WriteNull();                                         break;
                }
            }
        }
    }
}

#endif