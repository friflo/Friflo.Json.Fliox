// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
                        break;
                    case JsonEvent.ValueString:
                        WriteBytes(column, cell);
                        break;
                    case JsonEvent.ValueBool:
                        raw.sqlite3_bind_int(stmt, column.ordinal, cell.boolean ? 1 : 0);
                        break;
                    case JsonEvent.ValueNumber:
                        WriteNumber(column, cell);
                        break;
                    case JsonEvent.ArrayStart:
                        WriteBytes(column, cell);
                        break;
                    case JsonEvent.ObjectStart:
                        raw.sqlite3_bind_int(stmt, column.ordinal, cell.boolean ? 1 : 0);
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected cell.type: {cell.type}");
                }
                cell.type = JsonEvent.None;
            }
        }
        
        private void WriteBytes(ColumnInfo column, in RowCell cell) {
            var span    = cell.value.AsSpan();
            var rc      = raw.sqlite3_bind_text(stmt, column.ordinal, span);
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
                var rc = raw.sqlite3_bind_double(stmt, column.ordinal, value);
                if (rc != raw.SQLITE_OK) {
                    throw new InvalidOperationException($"WriteNumber(). error: {rc}");
                }
            } else {
                var value = ValueParser.ParseLong(cell.value.AsSpan(), ref json2Sql.parseError, out bool success);
                if (!success) {
                    throw new InvalidOperationException($"Json2SQL error: {json2Sql.parseError}");
                }
                var rc = raw.sqlite3_bind_int64(stmt, column.ordinal, value);
                if (rc != raw.SQLITE_OK) {
                    throw new InvalidOperationException($"WriteNumber(). error: {rc}");
                }
            }
        }
    }
}