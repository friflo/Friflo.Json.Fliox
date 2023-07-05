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

            var rowValues       = new RowValue[columns.Length];
            var context         = new TableContext(rowValues, tableInfo, processor);
            
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
                    var value = rowValues[n].str ?? "NULL";
                    sb.Append(value);
                    rowValues[n] = default;
                }
                sb.Append(')');
            }
        }
    }

    internal struct RowValue
    {
        internal string str;
    }

    internal class TableContext
    {
        private readonly    EntityProcessor    processor;
        private readonly    RowValue[]         rowValues;
        private readonly    TableInfo           tableInfo;
        
        internal TableContext(RowValue[] rowValues, TableInfo tableInfo, EntityProcessor processor) {
            this.rowValues  = rowValues;
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
                        var column = tableInfo.GetColumnOrdinal(ref parser);
                        rowValues[column.ordinal].str = $"'{parser.value.AsString()}'"; 
                        break;
                    }
                    case JsonEvent.ValueNumber: {
                        var column = tableInfo.GetColumnOrdinal(ref parser);
                        rowValues[column.ordinal].str = parser.value.AsString();
                        break;
                    }
                    case JsonEvent.ValueBool: {
                        var value = parser.boolValue ? "TRUE" : "FALSE";
                        var column = tableInfo.GetColumnOrdinal(ref parser);
                        rowValues[column.ordinal].str = value;
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