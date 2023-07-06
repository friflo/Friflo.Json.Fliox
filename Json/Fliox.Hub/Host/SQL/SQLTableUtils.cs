// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Utils;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable UseIndexFromEndExpression
// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public static class SQLTableUtils
    {
        private static readonly Bytes Null    = new Bytes("NULL");
        
        public static void AppendColumnValues(
            StringBuilder               sb2,
            List<JsonEntity>            entities,
            SQLEscape                   escape,
            TableInfo                   tableInfo,
            ObjectPool<SQLConverter>    sqlConverter)
        {
            sb2.Append(" (");
            var isFirst = true;
            var columns = tableInfo.columns;
            foreach (var column in columns) {
                if (isFirst) isFirst = false; else sb2.Append(',');
                sb2.Append(column.name);
            }
            sb2.Append(")\nVALUES");
            
            using var pooled    = sqlConverter.Get();
            var processor       = pooled.instance;
            var rowCells        = new RowCell[columns.Length];
            var context         = new TableContext(rowCells, processor);
            
            // var escaped = new StringBuilder();
            var sb = processor.sb;
            sb.Clear();
            var isFirstRow = true;
            foreach (var entity in entities)
            {
                if (isFirstRow) isFirstRow = false; else sb.AppendChar(',');
                processor.buffer.Clear();
                
                processor.parser.InitParser(entity.value);
                var ev = processor.parser.NextEvent();
                if (ev != JsonEvent.ObjectStart) throw new InvalidOperationException("expect object");
                context.Traverse(tableInfo.root);
                
                AddRowValues(ref sb, rowCells);
            }
            sb2.Append(sb.AsString());
            if ((escape & SQLEscape.BackSlash) != 0) {
                sb2.Replace("\\", "\\\\", 0, sb2.Length);
            }
        }
        
        private static void AddRowValues(ref Bytes sb, RowCell[] rowCells)
        {
            sb.AppendChar('(');
            var firstValue = true;
            for (int n = 0; n < rowCells.Length; n++) {
                if (firstValue) firstValue = false; else sb.AppendChar(',');
                ref var cell = ref rowCells[n];
                switch (cell.type) {
                    case JsonEvent.None:
                    case JsonEvent.ValueNull:
                        sb.AppendBytes(Null);
                        break;
                    case JsonEvent.ValueString:
                        sb.AppendChar('\'');
                        sb.AppendBytes(cell.value);
                        sb.AppendChar('\'');
                        break;
                    case JsonEvent.ValueNumber:
                        sb.AppendBytes(cell.value);
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected cell.type: {cell.type}");
                }
                cell.type = JsonEvent.None;
            }
            sb.AppendChar(')');
        }
    }

    internal struct RowCell
    {
        internal Bytes      value;
        internal JsonEvent  type;

        public override string ToString() => type == JsonEvent.None ? "None" : $"{value}: {type}";
    }

    internal class TableContext
    {
        private readonly    SQLConverter    processor;
        private readonly    RowCell[]       rowCells;
        
        private static readonly Bytes True    = new Bytes("TRUE");
        private static readonly Bytes False   = new Bytes("FALSE");
        
        internal TableContext(RowCell[] rowCells, SQLConverter processor) {
            this.rowCells   = rowCells;
            this.processor  = processor;
        }
        
        internal void Traverse(ObjectInfo objInfo)
        {
            ref var parser = ref processor.parser;
            while (true) {
                var ev = processor.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString: {
                        var column          = objInfo.FindColumn(parser.key);
                        ref var cell        = ref rowCells[column.ordinal];
                        processor.buffer.AppendBytes(parser.value);
                        cell.value.buffer   = processor.buffer.buffer;
                        var end             = processor.buffer.end;
                        cell.value.end      = end;
                        cell.value.start    = end - parser.value.Len; 
                        cell.type           = JsonEvent.ValueString;
                        break;
                    }
                    case JsonEvent.ValueNumber: {
                        var column          = objInfo.FindColumn(parser.key);
                        ref var cell        = ref rowCells[column.ordinal];
                        processor.buffer.AppendBytes(parser.value);
                        cell.value.buffer   = processor.buffer.buffer;
                        var end             = processor.buffer.end;
                        cell.value.end      = end;
                        cell.value.start    = end - parser.value.Len; 
                        cell.type           = JsonEvent.ValueNumber;
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
    }
}