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
        
        public static FieldType[] GetFieldTypes(DbDataReader reader) {
            var count   = reader.FieldCount;
            var types  = new FieldType[count];
            for (int n = 0; n < count; n++) {
                var type = reader.GetFieldType(n);
                FieldType fieldType = type switch {
                    Type _ when type == typeof(byte)        => FieldType.UInt8,
                    Type _ when type == typeof(sbyte)       => FieldType.Int16,
                    Type _ when type == typeof(short)       => FieldType.Int16,
                    Type _ when type == typeof(int)         => FieldType.Int32,
                    Type _ when type == typeof(long)        => FieldType.Int64,
                    Type _ when type == typeof(string)      => FieldType.String,
                    Type _ when type == typeof(DateTime)    => FieldType.DateTime,
                    Type _ when type == typeof(double)      => FieldType.Double,
                    Type _ when type == typeof(float)       => FieldType.Float,
                    _                                       => FieldType.None
                };
                types[n] = fieldType;
            }
            return types;
        }
        
        public static void AddRow(DbDataReader reader, FieldType[] fieldTypes, ref RawSqlResult result) {
            var count   = fieldTypes.Length;
            var values  = result.values;
            result.rows++;
            for (int n = 0; n < count; n++) {
                if (reader.IsDBNull(n)) {
                    continue;
                }
                var type = fieldTypes[n];
                JsonKey value = default;
                switch (type) {
                    case FieldType.UInt8:       value = new JsonKey(reader.GetByte(n));         break;
                    case FieldType.Int16:       value = new JsonKey(reader.GetInt16(n));        break;
                    case FieldType.Int32:       value = new JsonKey(reader.GetInt32(n));        break;
                    case FieldType.Int64:       value = new JsonKey(reader.GetInt64(n));        break;
                //  case FieldType.Float:       value = new JsonKey(reader.GetFloat(n));        break;
                //  case FieldType.Double:      value = new JsonKey(reader.GetDouble(n));       break;
                    case FieldType.String:      value = new JsonKey(reader.GetString(n));       break;
                //  case FieldType.DateTime:    value = new JsonKey(reader.GetDateTime(n));     break;
                    default:                    value = default;                                break;
                }
                values.Add(value);
            }
        }
    }

}