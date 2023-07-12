// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class Json2SQL : IDisposable
    {
        private     Utf8JsonParser  parser;
        private     ColumnInfo[]    columns;
        private     Bytes           buffer      = new Bytes(256);   // reused
        private     char[]          charBuffer  = new char[32];     // reused
        private     RowCell[]       rowCells    = new RowCell[4];   // reused

        private const           string  Null    = "NULL";
        private static readonly Bytes   True    = new Bytes("1");
        private static readonly Bytes   False   = new Bytes("0");

        public void AppendColumnValues(
            StringBuilder       sb,
            List<JsonEntity>    entities,
            SQLEscape           escape,
            TableInfo           tableInfo)
        {
            columns         = tableInfo.columns;
            var columnCount = columns.Length;
            if (columnCount > rowCells.Length) {
                rowCells = new RowCell[columnCount];
            }
            // var escaped = new StringBuilder();
            var isFirstRow = true;
            foreach (var entity in entities)
            {
                if (isFirstRow) isFirstRow = false; else sb.Append(",\n");
                buffer.Clear();
                
                parser.InitParser(entity.value);
                var ev = parser.NextEvent();
                if (ev != JsonEvent.ObjectStart) throw new InvalidOperationException("expect object");
                Traverse(tableInfo.root);
                
                AddRowValues(sb, columnCount, escape);
            }
        }
        
        private void Traverse(ObjectInfo objInfo)
        {
            while (true) {
                var ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString: {
                        var column      = objInfo.FindColumn(parser.key);
                        ref var cell    = ref rowCells[column.ordinal];
                        buffer.AppendBytes(parser.value);
                        cell.SetValue(buffer, parser.value.Len);
                        cell.type       = JsonEvent.ValueString;
                        break;
                    }
                    case JsonEvent.ValueNumber: {
                        var column      = objInfo.FindColumn(parser.key);
                        ref var cell    = ref rowCells[column.ordinal];
                        buffer.AppendBytes(parser.value);
                        cell.SetValue(buffer, parser.value.Len);
                        cell.type       = JsonEvent.ValueNumber;
                        break;
                    }
                    case JsonEvent.ValueBool: {
                        var column          = objInfo.FindColumn(parser.key);
                        ref var cell        = ref rowCells[column.ordinal];
                        cell.value          = parser.boolValue ? True : False;
                        cell.type           = JsonEvent.ValueBool;
                        break;
                    }
                    case JsonEvent.ArrayStart: {
                        var column          = objInfo.FindColumn(parser.key);
                        ref var cell        = ref rowCells[column.ordinal];
                        var start = parser.Position - 1;
                        parser.SkipTree(); // TODO implementation skipped for now
                        var end = parser.Position;
                        parser.AppendInputSlice(ref buffer, start, end);
                        cell.SetValue(buffer, end - start);
                        cell.type           = JsonEvent.ArrayStart;
                        break;
                    }
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ObjectStart:
                        var obj = objInfo.FindObject(parser.key);
                        if (obj != null) {
                            rowCells[obj.ordinal].type = JsonEvent.ObjectStart;
                            Traverse(obj);
                        } else {
                            parser.SkipTree();
                        }
                        break;
                    case JsonEvent.ObjectEnd:
                        return;
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
            }
        }
        
        private void AddRowValues(StringBuilder sb, int columnCount, SQLEscape escape)
        {
            sb.Append('(');
            var firstValue = true;
            for (int n = 0; n < columnCount; n++) {
                if (firstValue) firstValue = false; else sb.Append(',');
                ref var cell = ref rowCells[n];
                switch (cell.type) {
                    case JsonEvent.None:
                    case JsonEvent.ValueNull:
                        sb.Append(Null);
                        break;
                    case JsonEvent.ValueString:
                        if (columns[n].type == ColumnType.DateTime) {
                            AppendDateTime(sb, ref cell.value);
                        } else {
                            AppendString(sb, cell.value, escape);
                        }
                        break;
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNumber:
                        AppendBytes(sb, cell.value);
                        break;
                    case JsonEvent.ArrayStart:
                        sb.Append('\'');
                        AppendBytes(sb, cell.value);
                        sb.Append('\'');
                        break;
                    case JsonEvent.ObjectStart:
                        sb.Append('1');
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected cell.type: {cell.type}");
                }
                cell.type = JsonEvent.None;
            }
            sb.Append(')');
        }
        
        private void AppendString(StringBuilder sb, in Bytes value, SQLEscape escape) {
            if ((escape & SQLEscape.PrefixN) != 0) {
                sb.Append('N');
            }
            sb.Append('\'');
            var len = GetChars(value, out var chars);
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
                        case '\'':  sb.Append("''");   break;
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
        
        private static void AppendBytes(StringBuilder sb, in Bytes value) {
            var end = value.end;
            var buf = value.buffer;
            for (int n = value.start; n < end; n++) {
                sb.Append((char)buf[n]);
            }
        }
        
        private int GetChars(in Bytes bytes, out char[] chars) {
            var max = Encoding.UTF8.GetMaxCharCount(bytes.Len);
            if (max > charBuffer.Length) {
                charBuffer = new char[max];
            }
            chars = charBuffer;
            return Encoding.UTF8.GetChars(bytes.buffer, bytes.start, bytes.end - bytes.start, charBuffer, 0);
        }

        public void Dispose() {
            parser.Dispose();
        }
    }
    
    internal struct RowCell
    {
        internal Bytes      value;
        internal JsonEvent  type;
        
        internal void SetValue(in Bytes value, int len) {
            this.value.buffer   = value.buffer;
            var end             = value.end;
            this.value.end      = end;
            this.value.start    = end - len; 
        }

        public override string ToString() {
            switch (type) {
                case JsonEvent.None:        return "None";
                case JsonEvent.ObjectStart: return "Object";
                default:                    return $"{value}: {type}";
            }
        }
    }
}