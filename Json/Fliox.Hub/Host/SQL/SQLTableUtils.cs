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
            foreach (var column in tableInfo.columns) {
                if (isFirst) isFirst = false; else sb.Append(',');
                sb.Append(column.name);
            }
            sb.Append(") VALUES\n");
            
            using var pooled    = entityProcessor.Get();
            var processor       = pooled.instance;
            var context         = new TableContext(sb, escape, processor);
            
            // var escaped = new StringBuilder();
            isFirst = true;
            foreach (var entity in entities)
            {
                if (isFirst) isFirst = false; else sb.Append(',');
                sb.Append('(');
                processor.parser.InitParser(entity.value);
                context.Traverse();
                sb.Length -= 1; // remove last comma separator,
                sb.Append(')');
            }
        }
    }
    
    internal class TableContext
    {
        private readonly StringBuilder      sb;
        private readonly SQLEscape          escape;
        private readonly EntityProcessor    processor;
        
        internal TableContext(StringBuilder sb, SQLEscape escape, EntityProcessor processor) {
            this.sb         = sb;
            this.escape     = escape;
            this.processor  = processor;
        }
            
        internal void Traverse()
        {
            ref var parser = ref processor.parser;
            processor.parser.NextEvent();
            var ev = parser.Event;
            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                        sb.Append('\'');
                        sb.Append(parser.value.AsString());
                        sb.Append("',");
                        break;
                    case JsonEvent.ValueNumber:
                        sb.Append(parser.value.AsString());
                        sb.Append(',');
                        break;
                    case JsonEvent.ValueBool:
                        var value = parser.boolValue ? "TRUE," : "FALSE,";
                        sb.Append(value);
                        break;
                    case JsonEvent.ArrayStart:
                        break;
                    case JsonEvent.ValueNull:
                        sb.Append("NULL,");
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