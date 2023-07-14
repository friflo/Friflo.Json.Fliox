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
        public      RowCell[]       rowCells    = new RowCell[4];   // reused
        public      ColumnInfo[]    columns;
        public      Bytes           parseError  = new Bytes(8);
        // --- private
        private     Utf8JsonParser  parser;
        private     Bytes           buffer      = new Bytes(256);   // reused
        private     char[]          charBuffer  = new char[32];     // reused
        
        public void Dispose() {
            parser.Dispose();
        }

        public void AppendColumnValues(
            IJson2SQLWriter     writer,
            List<JsonEntity>    entities,
            TableInfo           tableInfo)
        {
            columns         = tableInfo.columns;
            var columnCount = columns.Length;
            if (columnCount > rowCells.Length) {
                rowCells = new RowCell[columnCount];
            }
            foreach (var entity in entities)
            {
                buffer.Clear();
                parser.InitParser(entity.value);
                var ev = parser.NextEvent();
                if (ev != JsonEvent.ObjectStart) throw new InvalidOperationException("expect object");
                Traverse(tableInfo.root);
                
                writer.AddRowValues(columnCount);
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
                        cell.isFloat    = parser.isFloat;
                        cell.type       = JsonEvent.ValueNumber;
                        break;
                    }
                    case JsonEvent.ValueBool: {
                        var column          = objInfo.FindColumn(parser.key);
                        ref var cell        = ref rowCells[column.ordinal];
                        cell.boolean        = parser.boolValue;
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
        
        public int GetChars(in Bytes bytes, out char[] chars) {
            var max = Encoding.UTF8.GetMaxCharCount(bytes.Len);
            if (max > charBuffer.Length) {
                charBuffer = new char[max];
            }
            chars = charBuffer;
            return Encoding.UTF8.GetChars(bytes.buffer, bytes.start, bytes.end - bytes.start, charBuffer, 0);
        }
    }
    
    public struct RowCell
    {
        public  Bytes       value;
        public  bool        boolean;
        public  bool        isFloat;
        public  JsonEvent   type;
        
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