// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    internal sealed class PostgresSQL2Json : ISQL2JsonMapper
    {
        private readonly    DbDataReader    reader;
        
        internal PostgresSQL2Json(DbDataReader reader) {
            this.reader = reader;
        }

        /// <summary>async version of <see cref="ReadEntitiesAsync"/></summary>
        public List<EntityValue> ReadEntitiesSync(SQL2Json sql2Json, TableInfo tableInfo, MemoryBuffer buffer)
        {
            sql2Json.InitMapper(this, tableInfo, buffer);
            while (reader.Read())
            {
                foreach (var column in tableInfo.columns) {
                    ReadCell(sql2Json, column, ref sql2Json.cells[column.ordinal]);
                }
                sql2Json.AddRow();
            }
            sql2Json.Cleanup();
            return sql2Json.result;
        }
        
        /// <summary>async version of <see cref="ReadEntitiesSync"/></summary>
        public async Task<List<EntityValue>> ReadEntitiesAsync(SQL2Json sql2Json, TableInfo tableInfo, MemoryBuffer buffer)
        {
            sql2Json.InitMapper(this, tableInfo, buffer);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                foreach (var column in tableInfo.columns) {
                    ReadCell(sql2Json, column, ref sql2Json.cells[column.ordinal]);
                }
                sql2Json.AddRow();
            }
            sql2Json.Cleanup();
            return sql2Json.result;
        }
    
        private void ReadCell(SQL2Json sql2Json, ColumnInfo column, ref ReadCell cell) {
            var ordinal = column.ordinal;
            if (reader.IsDBNull(ordinal)) {
                cell.type = ColumnType.None;
                return;
            }
            cell.type = column.type; 
            switch (column.type) {
                case ColumnType.Boolean:    cell.lng = reader.GetBoolean    (ordinal) ? 1 : 0;  return;
                //
                case ColumnType.String:
                case ColumnType.JsonKey:
                case ColumnType.Enum:
                case ColumnType.BigInteger: sql2Json.GetString(reader, ref cell, ordinal);      return;
                //
                case ColumnType.Uint8:      cell.lng = reader.GetByte       (ordinal);          return;
                case ColumnType.Int16:      cell.lng = reader.GetInt16      (ordinal);          return;
                case ColumnType.Int32:      cell.lng = reader.GetInt32      (ordinal);          return;
                case ColumnType.Int64:      cell.lng = reader.GetInt64      (ordinal);          return;
                //
                case ColumnType.Float:      cell.dbl = reader.GetFloat      (ordinal);          return;
                case ColumnType.Double:     cell.dbl = reader.GetDouble     (ordinal);          return;
                //
                case ColumnType.DateTime:   cell.date= DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc); return;
                case ColumnType.Guid:       cell.guid= reader.GetGuid       (ordinal);          return;
                //
                case ColumnType.JsonValue:
                case ColumnType.Array:      cell.str = reader.GetString     (ordinal);          return; // JSONB
                case ColumnType.Object:     cell.lng = reader.GetBoolean    (ordinal) ? 1 : 0;  return; // true => object is not null
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
        
        public void WriteJsonMember(SQL2Json sql2Json, ColumnInfo column)
        {
            ref var cell    = ref sql2Json.cells[column.ordinal];
            if (cell.type == ColumnType.None) {
                // writer.MemberNul(key); // omit writing member with value null
                return;
            }
            ref var writer  = ref sql2Json.writer;
            var key         = column.nameBytes.AsSpan();
            cell.type = column.type;
            switch (column.type) {
                case ColumnType.Boolean:    writer.MemberBln    (key, cell.lng != 0);               break;
                //
                case ColumnType.String:
                case ColumnType.JsonKey:
                case ColumnType.Enum:
                case ColumnType.BigInteger: writer.MemberStr    (key, cell.CharsSpan());            break;
                //
                case ColumnType.Uint8:
                case ColumnType.Int16:
                case ColumnType.Int32:
                case ColumnType.Int64:      writer.MemberLng    (key, cell.lng);                    break;
                //
                case ColumnType.Float:
                case ColumnType.Double:     writer.MemberDbl    (key, cell.dbl);                    break;
                //
                case ColumnType.Guid:       writer.MemberGuid   (key, cell.guid);                   break;
                case ColumnType.DateTime:   writer.MemberDate   (key, cell.date);                   break;
                case ColumnType.JsonValue:
                case ColumnType.Array:      writer.MemberArr(key, sql2Json.String2Bytes(cell.str)); break;
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
    }
}