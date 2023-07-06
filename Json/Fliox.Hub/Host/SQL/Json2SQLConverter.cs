// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class Json2SQLConverter : IDisposable
    {
        private     Utf8JsonParser  parser;
        private     Bytes           buffer      = new Bytes(256);
        private     char[]          charBuffer  = new char[32];
        private     RowCell[]       rowCells;

        private const           string  Null    = "NULL";
        private static readonly Bytes   True    = new Bytes("TRUE");
        private static readonly Bytes   False   = new Bytes("FALSE");

        public void AppendColumnValues(
            StringBuilder       sb,
            List<JsonEntity>    entities,
            SQLEscape           escape,
            TableInfo           tableInfo)
        {
            sb.Append(" (");
            var isFirst = true;
            var columns = tableInfo.columns;
            foreach (var column in columns) {
                if (isFirst) isFirst = false; else sb.Append(',');
                sb.Append(column.name);
            }
            sb.Append(")\nVALUES\n");

            rowCells    = new RowCell[columns.Length];
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
                
                AddRowValues(sb, rowCells, this);
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
                    case JsonEvent.ArrayStart:
                        break;
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ObjectStart:
                        var obj = objInfo.FindObject(parser.key);
                        if (obj != null) {
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
        
        private static void AddRowValues(StringBuilder sb, RowCell[] rowCells, Json2SQLConverter converter)
        {
            sb.Append('(');
            var firstValue = true;
            for (int n = 0; n < rowCells.Length; n++) {
                if (firstValue) firstValue = false; else sb.Append(',');
                ref var cell = ref rowCells[n];
                switch (cell.type) {
                    case JsonEvent.None:
                    case JsonEvent.ValueNull:
                        sb.Append(Null);
                        break;
                    case JsonEvent.ValueString:
                        AppendString(sb, cell.value, converter);
                        break;
                    case JsonEvent.ValueNumber:
                        AppendBytes(sb, cell.value);
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected cell.type: {cell.type}");
                }
                cell.type = JsonEvent.None;
            }
            sb.Append(')');
        }
        
        private static void AppendString(StringBuilder sb, in Bytes value, Json2SQLConverter converter) {
            sb.Append('\'');
            var len = converter.GetChars(value, out var chars);
            for (int n = 0; n < len; n++) {
                var c = chars[n];
                switch (c) {
                    case '\'':  sb.Append("\\'");   break;
                    case '\\':  sb.Append("\\\\");  break;
                    default:    sb.Append(c);       break;
                }
            }
            sb.Append('\'');
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

        public override string ToString() => type == JsonEvent.None ? "None" : $"{value}: {type}";
    }
}