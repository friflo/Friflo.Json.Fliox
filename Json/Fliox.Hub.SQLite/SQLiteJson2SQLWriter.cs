// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.SQL;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public class SQLiteJson2SQLWriter : IJson2SQLWriter
    {
        private readonly    Json2SQL        json2Sql;
        private readonly    sqlite3_stmt    stmt;

        public SQLiteJson2SQLWriter(Json2SQL json2Sql, sqlite3_stmt stmt) {
            this.json2Sql   = json2Sql;
            this.stmt       = stmt;
        }

        public void AddRowValues(int columnCount)
        {
            var columns     = json2Sql.columns;
            for (int n = 0; n < columnCount; n++) {
                var column = columns[n];
                ref var cell = ref json2Sql.rowCells[n];
                switch (cell.type) {
                    case JsonEvent.None:
                    case JsonEvent.ValueNull:
                        raw.sqlite3_bind_null(stmt, column.ordinal + 1);
                        break;
                    case JsonEvent.ValueString:
                        WriteBytes(column, cell);
                        break;
                    case JsonEvent.ValueBool:
                        raw.sqlite3_bind_int(stmt, column.ordinal + 1, cell.boolean ? 1 : 0);
                        break;
                    case JsonEvent.ValueNumber:
                        WriteNumber(column, cell);
                        break;
                    case JsonEvent.ArrayStart:
                        WriteBytes(column, cell);
                        break;
                    case JsonEvent.ObjectStart:
                        raw.sqlite3_bind_int(stmt, column.ordinal + 1, cell.boolean ? 1 : 0);
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected cell.type: {cell.type}");
                }
                cell.type = JsonEvent.None;
            }
            var rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_DONE) {
                // return Error($"step failed. error: {rc}, key: {key}", out error);
                throw new InvalidOperationException($"AddRowValues() step error: {rc}");
            }
            raw.sqlite3_reset(stmt);
        }
        
        private void WriteBytes(ColumnInfo column, in RowCell cell) {
            var span    = cell.value.AsSpan();
            var rc      = raw.sqlite3_bind_text(stmt, column.ordinal + 1, span);
            if (rc != raw.SQLITE_OK) {
                throw new InvalidOperationException($"WriteBytes(). error: {rc}");
            }
        }
        
        private void WriteNumber(ColumnInfo column, in RowCell cell) {
            if (cell.isFloat) {
                var value = ValueParser.ParseDoubleStd(cell.value.AsSpan(), ref json2Sql.parseError, out bool success);
                if (!success) {
                    throw new InvalidOperationException($"Json2SQL error: {json2Sql.parseError}");
                }
                var rc = raw.sqlite3_bind_double(stmt, column.ordinal + 1, value);
                if (rc != raw.SQLITE_OK) {
                    throw new InvalidOperationException($"WriteNumber(). error: {rc}");
                }
            } else {
                var value = ValueParser.ParseLong(cell.value.AsSpan(), ref json2Sql.parseError, out bool success);
                if (!success) {
                    throw new InvalidOperationException($"Json2SQL error: {json2Sql.parseError}");
                }
                var rc = raw.sqlite3_bind_int64(stmt, column.ordinal + 1, value);
                if (rc != raw.SQLITE_OK) {
                    throw new InvalidOperationException($"WriteNumber(). error: {rc}");
                }
            }
        }
    }
}

#endif