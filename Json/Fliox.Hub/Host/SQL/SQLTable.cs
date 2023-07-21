// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable ConvertTypeCheckPatternToNullCheck
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public interface ISQLDatabase
    {
        Task CreateFunctions                (ISyncConnection connection);
    }
    
    public interface ISQLTable
    {
        Task<SQLResult> CreateTable         (ISyncConnection connection);
        Task<SQLResult> AddVirtualColumns   (ISyncConnection connection);
        Task<SQLResult> AddColumns          (ISyncConnection connection);
    }
    
    public static class SQLTable
    {
        public static void AppendColumnNames(StringBuilder sb, TableInfo tableInfo) {
            var isFirst = true;
            var columns = tableInfo.columns;
            foreach (var column in columns) {
                if (isFirst) isFirst = false; else sb.Append(',');
                sb.Append(tableInfo.colStart);
                sb.Append(column.name);
                sb.Append(tableInfo.colEnd);
            }
        }
        
        public static void AppendValuesSQL(
            StringBuilder       sb,
            List<JsonEntity>    entities,
            SQLEscape           escape,
            TableInfo           tableInfo,
            SyncContext         syncContext)
        {
            using var pooled = syncContext.pool.Json2SQL.Get();
            sb.Append(" (");
            AppendColumnNames(sb, tableInfo);
            sb.Append(")\nVALUES\n");
            var writer = new Json2SQLWriter (pooled.instance, sb, escape);
            pooled.instance.AppendColumnValues(writer, entities, tableInfo);
        }
        
        public static async Task<ReadEntitiesResult> ReadEntitiesAsync(
            DbDataReader    reader,
            ReadEntities    query,
            TableInfo       tableInfo,
            SyncContext     syncContext)
        {
            using var pooled = syncContext.pool.SQL2Json.Get();
            var mapper   = new SQL2JsonMapper(reader);
            var buffer   = syncContext.MemoryBuffer;
            var entities = await mapper.ReadEntitiesAsync(pooled.instance, tableInfo, buffer).ConfigureAwait(false);
            var array    = KeyValueUtils.EntityListToArray(entities, query.ids);
            return new ReadEntitiesResult { entities = array };
        }
        
        public static async Task<List<EntityValue>> QueryEntitiesAsync(
            DbDataReader    reader,
            TableInfo       tableInfo,
            SyncContext     syncContext)
        {
            using var pooled = syncContext.pool.SQL2Json.Get();
            var mapper   = new SQL2JsonMapper(reader);
            var buffer   = syncContext.MemoryBuffer;
            return await mapper.ReadEntitiesAsync(pooled.instance, tableInfo, buffer).ConfigureAwait(false);
        }
        
        public static async Task<RawSqlResult> ReadRows(DbDataReader reader) {
            var columns     = GetFieldTypes(reader);
            var values      = new JsonArray();
            var readRawSql  = new ReadRawSql(reader);
            var rowCount    = 0;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                rowCount++;
                AddRow(reader, columns, values, readRawSql);
                values.WriteNewRow();
            }
            return new RawSqlResult(columns, values, rowCount);
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public static RawSqlColumn[] GetFieldTypes(DbDataReader reader) {
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
                    Type _ when type == typeof(string)      => reader.GetDataTypeName(n) == "JSON" ? RawColumnType.JSON : RawColumnType.String,
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
        public static void AddRow(DbDataReader reader, RawSqlColumn[] columns, JsonArray values, ReadRawSql rawSql) {
            var count = columns.Length;
            for (int n = 0; n < count; n++) {
                if (reader.IsDBNull(n)) {
                    values.WriteNull();
                    continue;
                }
                var type = columns[n].type;
                switch (type) {
                    case RawColumnType.Bool:        values.WriteBoolean     (reader.GetBoolean  (n));   break;
                    case RawColumnType.Uint8:       values.WriteByte        (reader.GetByte     (n));   break;
                    case RawColumnType.Int16:       values.WriteInt16       (reader.GetInt16    (n));   break;
                    case RawColumnType.Int32:       values.WriteInt32       (reader.GetInt32    (n));   break;
                    case RawColumnType.Int64:       values.WriteInt64       (reader.GetInt64    (n));   break;
                    case RawColumnType.Float:       values.WriteFlt32       (reader.GetFloat    (n));   break;
                    case RawColumnType.Double:      values.WriteFlt64       (reader.GetDouble   (n));   break;
                    case RawColumnType.String:      values.WriteCharString  (rawSql.GetString   (n));   break;
                    case RawColumnType.DateTime:    values.WriteDateTime    (reader.GetDateTime (n));   break;
                    case RawColumnType.Guid:        values.WriteGuid        (reader.GetGuid     (n));   break;
                    case RawColumnType.JSON:        values.WriteCharJSON    (rawSql.GetString   (n));   break;
                    default:                        values.WriteNull();                                 break;
                }
            }
        }
    }
    
    public class ReadRawSql
    {
        private             char[]          charBuf;
        private  readonly   DbDataReader    reader;
        
        public ReadRawSql(DbDataReader reader) {
            this.reader = reader; 
        }
        
        public ReadOnlySpan<char> GetString(int ordinal) {
            var len = (int)reader.GetChars(ordinal, 0, null, 0, 0);
            if (charBuf == null || len > charBuf.Length) {
                charBuf = new char[len + 32]; // +32 ensure buffer grows not too often
            }
            reader.GetChars(ordinal, 0, charBuf, 0, len);
            return new ReadOnlySpan<char>(charBuf, 0, len);
        }
    }
}