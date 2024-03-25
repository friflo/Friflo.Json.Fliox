// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public interface IJson2SQLWriter
    {
        SQLError WriteRowValues(int columnCount);
    }
    
    public sealed class Json2SQLWriter : IJson2SQLWriter
    {
        private readonly    StringBuilder   sb;
        private readonly    bool            hasBool;
        private readonly    SQLEscape       escape;
        private readonly    Json2SQL        json2Sql;
        private             bool            isFirstRow;
        
        private const   string  Null    = "NULL";
        private const   char    One     = '1';
        private const   char    Zero    = '0';
        private const   string  True    = "true";
        private const   string  False   = "false";
        
        public Json2SQLWriter(Json2SQL json2Sql, StringBuilder sb, SQLEscape escape) {
            this.json2Sql   = json2Sql;
            this.sb         = sb;
            this.escape     = escape;
            hasBool         = (escape & SQLEscape.HasBool) != 0;
            isFirstRow      = true;
        }

        public SQLError WriteRowValues(int columnCount)
        {
            if (isFirstRow) {
                isFirstRow = false;
            } else {
                sb.Append(",\n");
            }
            var firstValue  = true;
            var columns     = json2Sql.columns;
            var cells       = json2Sql.rowCells;
            sb.Append('(');
            for (int n = 0; n < columnCount; n++) {
                if (firstValue) firstValue = false; else sb.Append(',');
                ref var cell = ref cells[n];
                switch (cell.type) {
                    case CellType.Null:
                        sb.Append(Null);
                        break;
                    case CellType.String:
                        if (columns[n].type == ColumnType.DateTime) {
                            AppendDateTime(sb, ref cell.value);
                        } else {
                            AppendString(sb, cell.value, escape);
                        }
                        break;
                    case CellType.Bool:
                        AppendBool(sb, cell.boolean);
                        break;
                    case CellType.Number:
                        AppendBytes(sb, cell.value);
                        break;
                    case CellType.JSON:
                    case CellType.Array:
                        sb.Append('\'');
                        AppendBytes(sb, cell.value);
                        sb.Append('\'');
                        break;
                    case CellType.Object:
                        if (hasBool) {
                            sb.Append(True);
                        } else {
                            sb.Append(One);
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected cell.type: {cell.type}");
                }
                cell.type = CellType.Null;
            }
            sb.Append(')');
            return default;
        }
        
        
        private void AppendString(StringBuilder sb, in Bytes value, SQLEscape escape) {
            if ((escape & SQLEscape.PrefixN) != 0) {
                sb.Append('N');
            }
            sb.Append('\'');
            var len = json2Sql.GetChars(value, out var chars);
            if ((escape & SQLEscape.BackSlash) != 0) {
                for (int n = 0; n < len; n++) {
                    var c = chars[n];
                    switch (c) {
                        case '\'':  sb.Append("\\'");   break;
                        case '\\':  sb.Append("\\\\");  break;
                        default:    sb.Append(c);       break;
                    }
                }
            } else {
                for (int n = 0; n < len; n++) {
                    var c = chars[n];
                    switch (c) {
                        case '\'':  sb.Append("''");    break;
                        default:    sb.Append(c);       break;
                    }
                }
            }
            sb.Append('\'');
        }
        
        private void AppendDateTime(StringBuilder sb, ref Bytes bytes) {
            var start   = bytes.start;
            var buf     = bytes.buffer;
            var len     = bytes.Len;
            // convert  "2022-01-01T00:00:00.000Z"
            // to       "2022-01-01 00:00:00.000"
            if (buf[bytes.end -1] == 'Z') {
                bytes.end--;
            }
            if (len > 10) {
                buf[start + 10] = (byte)' ';
            }
            AppendString(sb, bytes, SQLEscape.Default);
        }
        
        private void AppendBool(StringBuilder sb, bool value) {
            if (hasBool) {
                if (value) {
                    sb.Append(True);
                } else {
                    sb.Append(False);
                }
            } else {
                if (value) {
                    sb.Append(One);
                } else {
                    sb.Append(Zero);
                }
            }
        }
        
        private static void AppendBytes(StringBuilder sb, in Bytes value) {
            var end = value.end;
            var buf = value.buffer;
            for (int n = value.start; n < end; n++) {
                sb.Append((char)buf[n]);
            }
        }
    }
}