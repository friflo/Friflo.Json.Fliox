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
        public static void AppendColumnValues(
            StringBuilder               sb,
            List<JsonEntity>            entities,
            SQLEscape                   escape,
            TableInfo                   tableInfo,
            ObjectPool<EntityProcessor> entityProcessor)
        {
            sb.Append(" (");
            var isFirst = true;
            var columns = tableInfo.columns;
            foreach (var column in columns) {
                if (isFirst) isFirst = false; else sb.Append(',');
                sb.Append(column.name);
            }
            sb.Append(")\nVALUES");
            
            using var pooled    = entityProcessor.Get();
            var processor       = pooled.instance;

            var rowCells        = new RowCell[columns.Length];
            var context         = new TableContext(rowCells, tableInfo, processor);
            
            // var escaped = new StringBuilder();
            var isFirstRow = true;
            foreach (var entity in entities)
            {
                if (isFirstRow) isFirstRow = false; else sb.Append(',');
                sb.Append('(');
                processor.parser.InitParser(entity.value);
                context.Traverse();
                var firstValue = true;
                for (int n = 0; n < columns.Length; n++) {
                    if (firstValue) firstValue = false; else sb.Append(',');
                    var cell = rowCells[n];
                    switch (cell.type) {
                        case JsonEvent.None:
                        case JsonEvent.ValueNull:
                            sb.Append("NULL");
                            break;
                        case JsonEvent.ValueString:
                            sb.Append('\'');
                            cell.value.AppendTo(sb);
                            sb.Append('\'');
                            break;
                        case JsonEvent.ValueNumber:
                            cell.value.AppendTo(sb);
                            break;                        
                    }
                    rowCells[n] = default;
                }
                sb.Append(')');
            }
        }
    }

    internal struct RowCell
    {
        internal ShortString    value;
        internal JsonEvent      type;

        public override string ToString() => $"{value}: {type}";
    }

    internal class TableContext
    {
        private readonly    EntityProcessor    processor;
        private readonly    RowCell[]          rowCells;
        private readonly    TableInfo          tableInfo;
        
        private static readonly ShortString True    = new ShortString("TRUE");
        private static readonly ShortString False   = new ShortString("FALSE");
        
        internal TableContext(RowCell[] rowCells, TableInfo tableInfo, EntityProcessor processor) {
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
                        var column      = tableInfo.GetColumnOrdinal(ref parser);
                        ref var cell    = ref rowCells[column.ordinal];
                        cell.value      = new ShortString(parser.value, null);
                        cell.type       = JsonEvent.ValueString;
                        break;
                    }
                    case JsonEvent.ValueNumber: {
                        var column      = tableInfo.GetColumnOrdinal(ref parser);
                        ref var cell    = ref rowCells[column.ordinal];
                        cell.value      = new ShortString(parser.value, null);
                        cell.type       = JsonEvent.ValueNumber;
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