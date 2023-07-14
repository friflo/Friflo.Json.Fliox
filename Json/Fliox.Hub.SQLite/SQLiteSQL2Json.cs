using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public class SQLiteSQL2Json : ISQL2JsonMapper
    {
        private readonly    sqlite3_stmt        stmt;
        private readonly    SQL2Json            sql2Json;
        private readonly    TableInfo           tableInfo;
        
        public SQLiteSQL2Json(SQL2Json sql2Json, sqlite3_stmt stmt, TableInfo tableInfo) {
            this.sql2Json   = sql2Json;
            this.stmt       = stmt;
            this.tableInfo  = tableInfo;
        }
        
        internal bool ReadValues(
            int?                    maxCount,                     
            out TaskExecuteError    error)
        {
            int count   = 0;
            var columns = tableInfo.columns;
            var cells   = sql2Json.cells;
            sql2Json.InitMapper(this, tableInfo);
            while (true) {
                var rc = raw.sqlite3_step(stmt);
                if (rc == raw.SQLITE_ROW) {
                    for (int n = 0; n < columns.Length; n++) {
                        ReadCell(columns[n], ref cells[n]);
                    }
                    sql2Json.AddRow();
                } else if (rc == raw.SQLITE_DONE) {
                    break;
                } else {
                    return SQLiteUtils.Error("step failed", out error);
                }
            }
            return SQLiteUtils.Success(out error);
        }
        
        public List<EntityValue> ReadEntities(TableInfo tableInfo)
        {
            /* sql2Json.InitMapper(this, tableInfo);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                foreach (var column in tableInfo.columns) {
                    ReadCell(sql2Json, column, ref sql2Json.cells[column.ordinal]);
                }
                sql2Json.AddRow();
            } */
            return sql2Json.result;
        }
    
        private void ReadCell(ColumnInfo column, ref ReadCell cell) {
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
                        throw new InvalidOperationException("invalid guid");
                    }
                    break;
                }
                case ColumnType.DateTime: {
                    var data = raw.sqlite3_column_blob(stmt, column.ordinal);
                    if (!Bytes.TryParseDateTime(data, out cell.date)) {
                        throw new InvalidOperationException("invalid datetime");
                    }
                    break;
                }
                case ColumnType.BigInteger:
                case ColumnType.String:
                case ColumnType.Enum:
                case ColumnType.Array: {
                    var data = raw.sqlite3_column_blob(stmt, column.ordinal);
                    sql2Json.CopyBytes(data, ref cell);
                    break;
                }
            }
        }
        
        public void WriteColumn(SQL2Json sql2Json, ColumnInfo column)
        {
            ref var cell    = ref sql2Json.cells[column.ordinal];
            ref var writer  = ref sql2Json.writer;
            var key         = column.nameBytes;
            if (cell.isNull) {
                // writer.MemberNul(key); // omit writing member with value null
                return;
            }
            cell.isNull = true;
            switch (column.type) {
                case ColumnType.Boolean:    writer.MemberBln    (key, cell.lng != 0);   break;
                //
                case ColumnType.String:
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