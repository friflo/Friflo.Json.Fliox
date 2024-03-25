// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        private     ColumnInfo      keyColumn;
        private     Utf8JsonParser  parser;
        private     Bytes           buffer      = new Bytes(256);   // reused
        private     char[]          charBuffer  = new char[32];     // reused
        
        private static readonly Bytes True  = new Bytes("true");
        private static readonly Bytes False = new Bytes("false");
        
        public void Dispose() {
            parser.Dispose();
        }

        public SQLError AppendColumnValues(
            IJson2SQLWriter     writer,
            List<JsonEntity>    entities,
            TableInfo           tableInfo)
        {
            columns         = tableInfo.columns;
            keyColumn       = tableInfo.keyColumn;
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
                
                var error = writer.WriteRowValues(columnCount);
                if (error.message is not null) {
                    return error;
                }
            }
            return default;
        }
        
        private void Traverse(ObjectInfo objInfo)
        {
            var cells = rowCells;
            while (true) {
                var ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString: {
                        var column      = objInfo.FindColumn(parser.key);
                        ref var cell    = ref cells[column.ordinal];
                        if (column.type == ColumnType.JsonValue) {
                            buffer.AppendChar('\"');
                            buffer.AppendBytes(parser.value);
                            buffer.AppendChar('\"');
                            cell.SetValue(buffer, parser.value.Len + 2); // + 2 for start and end "
                            cell.type       = CellType.JSON;
                        } else {
                            buffer.AppendBytes(parser.value);
                            cell.SetValue(buffer, parser.value.Len);
                            cell.type       = CellType.String;
                        }
                        break;
                    }
                    case JsonEvent.ValueNumber: {
                        var column      = objInfo.FindColumn(parser.key);
                        ref var cell    = ref cells[column.ordinal];
                        buffer.AppendBytes(parser.value);
                        cell.SetValue(buffer, parser.value.Len);
                        cell.isFloat    = parser.isFloat;
                        cell.type       = column.type == ColumnType.JsonValue ? CellType.JSON : CellType.Number;
                        break;
                    }
                    case JsonEvent.ValueBool: {
                        var column          = objInfo.FindColumn(parser.key);
                        ref var cell        = ref cells[column.ordinal];
                        if (column.type == ColumnType.JsonValue) {
                            if (parser.boolValue) {
                                buffer.AppendBytes(True);
                            } else {
                                buffer.AppendBytes(False);
                            }
                            cell.SetValue(buffer, parser.value.Len);
                            cell.type           = CellType.JSON;
                        } else {
                            cell.boolean        = parser.boolValue;
                            cell.type           = CellType.Bool;
                        }
                        break;
                    }
                    case JsonEvent.ArrayStart: {
                        var column          = objInfo.FindColumn(parser.key);
                        ref var cell        = ref cells[column.ordinal];
                        var start = parser.Position - 1;
                        parser.SkipTree();
                        var end     = parser.Position;
                        var json    = parser.GetInputBytes(start, end);
                        cell.SetValue(json, end - start);
                        cell.type   = CellType.Array;
                        break;
                    }
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ObjectStart:
                        var obj = objInfo.FindObject(parser.key);
                        if (obj != null) {
                            cells[obj.ordinal].type = CellType.Object;
                            Traverse(obj);
                            break;
                        }
                        var objColumn = objInfo.FindColumn(parser.key);
                        if (objColumn == null) {
                            parser.SkipTree();
                        } else {
                            ref var cell        = ref cells[objColumn.ordinal];
                            var start = parser.Position - 1;
                            parser.SkipTree();
                            var end     = parser.Position;
                            var json    = parser.GetInputBytes(start, end);
                            cell.SetValue(json, end - start);
                            cell.type   = CellType.JSON;
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
        
        /// <summary>Return the primary key of the current row</summary>
        public JsonKey DebugKey() {
            var bytes = rowCells[keyColumn.ordinal].value;
            return new JsonKey(bytes, default);
        }
    }
    
    public struct RowCell
    {
        public  Bytes       value;
        public  bool        boolean;
        public  bool        isFloat;
        public  CellType    type;
        
        internal void SetValue(in Bytes value, int len) {
            this.value.buffer   = value.buffer;
            var end             = value.end;
            this.value.end      = end;
            this.value.start    = end - len; 
        }

        public override string ToString() {
            switch (type) {
                case CellType.Null:     return "null";
                case CellType.Object:   return "Object";
                default:                return $"{value}: {type}";
            }
        }
    }
    
    public enum CellType
    {
        Null    = 0,
        String  = 1,
        Number  = 2,
        Bool    = 3,
        Array   = 4,
        Object  = 5,
        JSON    = 6,
    }
}