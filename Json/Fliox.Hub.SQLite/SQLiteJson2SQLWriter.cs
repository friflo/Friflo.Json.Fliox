// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.SQL;
using SQLitePCL;
using static Friflo.Json.Fliox.Hub.SQLite.SQLiteUtils;

// ReSharper disable TooWideLocalVariableScope
namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal class SQLiteJson2SQLWriter : IJson2SQLWriter
    {
        private readonly    Json2SQL        json2Sql;
        private readonly    SyncConnection  connection;
        private readonly    sqlite3_stmt    stmt;

        internal SQLiteJson2SQLWriter(Json2SQL json2Sql, SyncConnection connection, sqlite3_stmt stmt) {
            this.json2Sql   = json2Sql;
            this.connection = connection;
            this.stmt       = stmt;
        }

        public SQLError WriteRowValues(int columnCount)
        {
            var         columns = json2Sql.columns;
            var         cells   = json2Sql.rowCells;
            SQLError    error;
            for (int n = 0; n < columnCount; n++) {
                var column = columns[n];
                ref var cell = ref cells[n];
                switch (cell.type) {
                    case CellType.Null:     error = WriteNull   (column);       break;
                    case CellType.String:   error = WriteBytes  (column, cell); break;
                    case CellType.Bool:     error = WriteBool   (column, cell); break;
                    case CellType.Number:   error = WriteNumber (column, cell); break;
                    case CellType.Array:    error = WriteBytes  (column, cell); break;
                    case CellType.Object:   error = WriteBool   (column, cell); break;
                    case CellType.JSON:     error = WriteBytes  (column, cell); break;
                    default:
                        throw new InvalidOperationException($"unexpected cell.type: {cell.type}");
                }
                if (error.message is not null) {
                    return error;
                }
                cell.type = CellType.Null;
            }
            var rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_DONE) {
                var msg = GetErrorMsg("step failed.", connection, rc, json2Sql.DebugKey());
                return new SQLError(msg);
            }
            raw.sqlite3_reset(stmt);
            return default;
        }
        
        private SQLError WriteNull(ColumnInfo column) {
            var rc      = raw.sqlite3_bind_null(stmt, column.ordinal + 1);
            if (rc == raw.SQLITE_OK) {
                return default;
            }
            var msg = GetErrorMsg("write null failed.", connection, rc, json2Sql.DebugKey());
            return new SQLError(msg);
        }
        
        private SQLError WriteBytes(ColumnInfo column, in RowCell cell) {
            var span    = cell.value.AsSpan();
            var rc      = raw.sqlite3_bind_text(stmt, column.ordinal + 1, span);
            if (rc == raw.SQLITE_OK) {
                return default;
            }
            var msg = GetErrorMsg("bind failed.", connection, rc, json2Sql.DebugKey());
            return new SQLError(msg);
        }
        
        private SQLError WriteNumber(ColumnInfo column, in RowCell cell) {
            if (cell.isFloat) {
                var value = ValueParser.ParseDoubleStd(cell.value.AsSpan(), ref json2Sql.parseError, out bool success);
                if (!success) {
                    return new SQLError($"parsing floating point number failed. error: {json2Sql.parseError}, PK: {json2Sql.DebugKey()}");
                }
                var rc = raw.sqlite3_bind_double(stmt, column.ordinal + 1, value);
                if (rc == raw.SQLITE_OK) {
                    return default;
                }
                var msg = GetErrorMsg("bind double failed.", connection, rc, json2Sql.DebugKey());
                return new SQLError(msg);
            } else {
                var value = ValueParser.ParseLong(cell.value.AsSpan(), ref json2Sql.parseError, out bool success);
                if (!success) {
                    return new SQLError($"parsing integer failed. error: {json2Sql.parseError}, PK: {json2Sql.DebugKey()}");
                }
                var rc = raw.sqlite3_bind_int64(stmt, column.ordinal + 1, value);
                if (rc == raw.SQLITE_OK) {
                    return default;
                }
                var msg = GetErrorMsg("bind int64 failed.", connection, rc, json2Sql.DebugKey());
                return new SQLError(msg);
            }
        }
        
        private SQLError WriteBool(ColumnInfo column, in RowCell cell) {
            var rc = raw.sqlite3_bind_int(stmt, column.ordinal + 1, cell.boolean ? 1 : 0);
            if (rc == raw.SQLITE_OK) {
                return default;
            }
            var msg = GetErrorMsg("bind int failed.", connection, rc, json2Sql.DebugKey());
            return new SQLError(msg);
        }
    }
}

#endif