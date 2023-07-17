// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Utils;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal sealed class SQLiteSQL2Json : ISQL2JsonMapper
    {
        private readonly    sqlite3_stmt        stmt;
        private readonly    SQL2Json            sql2Json;
        private readonly    TableInfo           tableInfo;
        
        internal SQLiteSQL2Json(SQL2Json sql2Json, sqlite3_stmt stmt, TableInfo tableInfo) {
            this.sql2Json   = sql2Json;
            this.stmt       = stmt;
            this.tableInfo  = tableInfo;
        }
        
        internal bool ReadValues(
            int?                    maxCount,                     
            MemoryBuffer            buffer,
            out TaskExecuteError    error)
        {
            sql2Json.InitMapper(this, tableInfo, buffer);
            int count   = 0;
            var columns = tableInfo.columns;
            var cells   = sql2Json.cells;
            while (true) {
                var rc = raw.sqlite3_step(stmt);
                if (rc == raw.SQLITE_ROW) {
                    for (int n = 0; n < columns.Length; n++) {
                        var column      = columns[n];
                        ref var cell    = ref cells[n];
                        if (raw.sqlite3_column_type(stmt, column.ordinal) == raw.SQLITE_NULL) {
                            cell.type = ColumnType.None;
                            continue;
                        } 
                        var sqlError = ReadCell(column, ref cell);
                        if (sqlError.message is not null) {
                            SQLiteUtils.Error(sqlError.message, out error);
                            return false;
                        }
                    }
                    sql2Json.AddRow();
                    count++;
                    if (maxCount != null && count >= maxCount) {
                        return SQLiteUtils.Success(out error);
                    }
                } else if (rc == raw.SQLITE_DONE) {
                    break;
                } else {
                    return SQLiteUtils.Error($"step failed. PK: {sql2Json.DebugKey()}", out error);
                }
            }
            sql2Json.Cleanup();
            return SQLiteUtils.Success(out error);
        }
        
        private static int BindKey(sqlite3_stmt stmt, in JsonKey key, ref Bytes bytes) {
            var encoding = key.GetEncoding();
            switch (encoding) {
                case JsonKeyEncoding.LONG:
                    return raw.sqlite3_bind_int64(stmt, 1, key.AsLong());
                case JsonKeyEncoding.STRING:
                    return raw.sqlite3_bind_text (stmt, 1, key.AsString());
                case JsonKeyEncoding.STRING_SHORT:
                case JsonKeyEncoding.GUID:
                    key.ToBytes(ref bytes);
                    return raw.sqlite3_bind_text (stmt, 1, bytes.AsSpan());
                default:
                    throw new InvalidOperationException($"unhandled case: {encoding}");
            }
        }
        
        internal bool ReadEntities(List<JsonKey> keys, MemoryBuffer buffer, out TaskExecuteError error)
        {
            sql2Json.InitMapper(this, tableInfo, buffer);
            var columns = tableInfo.columns;
            var cells   = sql2Json.cells;
            var bytes   = new Bytes(36);        // TODO - OPTIMIZE: reuse

            foreach (var key in keys) {
                var rc  = BindKey(stmt, key, ref bytes);
                if (rc != raw.SQLITE_OK) {
                    return SQLiteUtils.Error($"bind key failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_step(stmt);
                switch (rc) {
                    case raw.SQLITE_DONE: 
                        sql2Json.result.Add(new EntityValue(key));
                        break;
                    case raw.SQLITE_ROW:
                        for (int n = 0; n < columns.Length; n++) {
                            var column = columns[n];
                            if (raw.sqlite3_column_type(stmt, column.ordinal) == raw.SQLITE_NULL) {
                                cells[n].type = ColumnType.None;
                                continue;
                            } 
                            var sqlError = ReadCell(column, ref cells[n]);
                            if (sqlError.message is not null) {
                                return SQLiteUtils.Error(sqlError.message, out error);
                            }
                        }
                        sql2Json.AddRow();
                        break;
                    default:
                        return SQLiteUtils.Error($"step failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_reset(stmt);
                if (rc != raw.SQLITE_OK) {
                    return SQLiteUtils.Error($"reset failed. error: {rc}, key: {key}", out error);
                }
            }
            sql2Json.Cleanup();
            return SQLiteUtils.Success(out error);
        }
    
        private SQLError ReadCell(ColumnInfo column, ref ReadCell cell)
        {
            cell.type = column.type;
            switch (column.type) {
                case ColumnType.Boolean:
                case ColumnType.Uint8:
                case ColumnType.Int16:
                case ColumnType.Int32:
                case ColumnType.Int64:
                case ColumnType.Object:
                    cell.lng = raw.sqlite3_column_int64(stmt, column.ordinal);
                    break;
                case ColumnType.Float:
                case ColumnType.Double:
                    cell.dbl = raw.sqlite3_column_double(stmt, column.ordinal);
                    break;
                case ColumnType.Guid: {
                    var data = raw.sqlite3_column_blob(stmt, column.ordinal);
                    if (!Bytes.TryParseGuid(data, out cell.guid)) {
                        var guidStr = new Bytes(data).ToString();
                        return new SQLError($"invalid guid: {guidStr}, PK: {sql2Json.DebugKey()}");
                    }
                    break;
                }
                case ColumnType.DateTime: {
                    var text = raw.sqlite3_column_blob(stmt, column.ordinal);
                    if (!Bytes.TryParseDateTime(text, out var dateTime)) {
                        var dateStr = new Bytes(text).ToString();
                        return new SQLError($"invalid datetime: {dateStr}, PK: {sql2Json.DebugKey()}");
                    }
                    cell.date = dateTime.ToUniversalTime();
                    break;
                }
                case ColumnType.BigInteger:
                case ColumnType.String:
                case ColumnType.JsonKey:
                case ColumnType.Enum:
                case ColumnType.Array: {
                    var data = raw.sqlite3_column_blob(stmt, column.ordinal);
                    sql2Json.CopyToCellBytes(data, ref cell);
                    break;
                }
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
            return default;
        }
        
        public void WriteJsonMember(SQL2Json sql2Json, ColumnInfo column)
        {
            ref var cell    = ref sql2Json.cells[column.ordinal];
            ref var writer  = ref sql2Json.writer;
            var key         = column.nameBytes;
            if (cell.type == ColumnType.None) {
                // writer.MemberNul(key); // omit writing member with value null
                return;
            }
            cell.type = column.type;
            switch (column.type) {
                case ColumnType.Boolean:    writer.MemberBln    (key, cell.lng != 0);   break;
                //
                case ColumnType.String:
                case ColumnType.JsonKey:
                case ColumnType.Enum:
                case ColumnType.BigInteger: writer.MemberStr    (key, cell.bytes);      break;
                //
                case ColumnType.Uint8:
                case ColumnType.Int16:
                case ColumnType.Int32:
                case ColumnType.Int64:      writer.MemberLng    (key, cell.lng);        break;
                //
                case ColumnType.Float:
                case ColumnType.Double:     writer.MemberDbl    (key, cell.dbl);        break;
                //
                case ColumnType.Guid:       writer.MemberGuid   (key, cell.guid);       break;
                case ColumnType.DateTime:   writer.MemberDate   (key, cell.date);       break;
                case ColumnType.Array:      writer.MemberArr    (key, cell.bytes);      break;
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
    }
}

#endif
