// Copyright (c) Ullrich Praetz. All rights reserved.
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
        internal static async Task<RawSqlResult> ReadRows(DbDataReader reader) {
            var types       = GetFieldTypes(reader);
            var values      = new JsonArray();
            var readRawSql  = new ReadRawSql(reader);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                AddRow(reader, types, values, readRawSql);
            }
            return new RawSqlResult(types, values);
        }
        
        
        // ReSharper disable once MemberCanBePrivate.Global
        private static FieldType[] GetFieldTypes(DbDataReader reader) {
            var count   = reader.FieldCount;
            var types  = new FieldType[count];
            for (int n = 0; n < count; n++) {
                var type = reader.GetFieldType(n);
                FieldType fieldType = type switch {
                    Type _ when type == typeof(bool)        => FieldType.Bool,
                    //
                    Type _ when type == typeof(byte)        => FieldType.UInt8,
                    Type _ when type == typeof(sbyte)       => FieldType.Int16,
                    Type _ when type == typeof(short)       => FieldType.Int16,
                    Type _ when type == typeof(int)         => FieldType.Int32,
                    Type _ when type == typeof(long)        => FieldType.Int64,
                    //
                    Type _ when type == typeof(string)      => reader.GetDataTypeName(n) == "jsonb" ? FieldType.JSON : FieldType.String,
                    Type _ when type == typeof(DateTime)    => FieldType.DateTime,
                    Type _ when type == typeof(Guid)        => FieldType.Guid,
                    //
                    Type _ when type == typeof(double)      => FieldType.Double,
                    Type _ when type == typeof(float)       => FieldType.Float,
                    //
                    _                                       => FieldType.Unknown
                };
                types[n] = fieldType;
            }
            return types;
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        private static void AddRow(DbDataReader reader, FieldType[] fieldTypes, JsonArray values, ReadRawSql rawSql) {
            var count   = fieldTypes.Length;
            for (int n = 0; n < count; n++) {
                if (reader.IsDBNull(n)) {
                    values.WriteNull();
                    continue;
                }
                var type = fieldTypes[n];
                switch (type) {
                    case FieldType.Bool:        values.WriteBoolean     (reader.GetBoolean  (n));           break;
                    case FieldType.UInt8:       values.WriteByte        (reader.GetByte     (n));           break;
                    case FieldType.Int16:       values.WriteInt16       (reader.GetInt16    (n));           break;
                    case FieldType.Int32:       values.WriteInt32       (reader.GetInt32    (n));           break;
                    case FieldType.Int64:       values.WriteInt64       (reader.GetInt64    (n));           break;
                    case FieldType.Float:       values.WriteFlt32       (reader.GetFloat    (n));           break;
                    case FieldType.Double:      values.WriteFlt64       (reader.GetDouble   (n));           break;
                    case FieldType.String:      values.WriteCharString  (rawSql.GetString   (n));           break;
                    case FieldType.DateTime:    values.WriteDateTime    (reader.GetDateTime (n));           break;
                    case FieldType.Guid:        values.WriteGuid        (reader.GetGuid     (n));           break;
                    case FieldType.JSON:        values.WriteCharJSON    (reader.GetString   (n).AsSpan());  break;
                    default:                    values.WriteNull();                                         break;
                }
            }
        }
    }
}

#endif