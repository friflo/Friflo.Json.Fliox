// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Utils;

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
            var context         = new TableContext(rowCells, tableInfo, processor);
            
            // var escaped = new StringBuilder();
            var sb = processor.sb;
            sb.Clear();
            var isFirstRow = true;
            foreach (var entity in entities)
            {
                if (isFirstRow) isFirstRow = false; else sb.AppendChar(',');
                sb.AppendChar('(');
                processor.buffer.Clear();
                processor.parser.InitParser(entity.value);
                context.Traverse();
                var firstValue = true;
                for (int n = 0; n < columns.Length; n++) {
                    if (firstValue) firstValue = false; else sb.AppendChar(',');
                    var cell = rowCells[n];
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
                    }
                    rowCells[n] = default;
                }
                sb.AppendChar(')');
            }
            sb2.Append(sb.AsString());
            if ((escape & SQLEscape.BackSlash) != 0) {
                sb2.Replace("\\", "\\\\", 0, sb2.Length);
            }
        }
    }

    internal struct RowCell
    {
        internal Bytes      value;
        internal JsonEvent  type;

        public override string ToString() => $"{value}: {type}";
    }

    internal class TableContext
    {
        private readonly    SQLConverter    processor;
        private readonly    RowCell[]       rowCells;
        private readonly    TableInfo       tableInfo;
        
        private static readonly Bytes True    = new Bytes("TRUE");
        private static readonly Bytes False   = new Bytes("FALSE");
        
        internal TableContext(RowCell[] rowCells, TableInfo tableInfo, SQLConverter processor) {
            this.rowCells   = rowCells;
            this.tableInfo  = tableInfo;
            this.processor  = processor;
        }
        
        internal void Traverse()
        {
            ref var parser = ref processor.parser;
            processor.parser.NextEvent();
            var ev = parser.Event;
            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString: {
                        var column          = tableInfo.GetColumnOrdinal(ref parser);
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
                        var column          = tableInfo.GetColumnOrdinal(ref parser);
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
                        var column      = tableInfo.GetColumnOrdinal(ref parser);
                        ref var cell    = ref rowCells[column.ordinal];
                        cell.value      = parser.boolValue ? True : False;
                        cell.type       = JsonEvent.ValueBool;
                        break;
                    }
                    case JsonEvent.ArrayStart:
                        break;
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ObjectStart:
                        Traverse();
                        break;
                    case JsonEvent.EOF:
                    case JsonEvent.ObjectEnd:
                        return;
                }
                ev = processor.parser.NextEvent();
            }
        }
    }
}