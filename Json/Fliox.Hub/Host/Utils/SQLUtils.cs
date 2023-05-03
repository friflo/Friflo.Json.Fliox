// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    public static class SQLUtils
    {
        public static string QueryEntities(QueryEntities command, string table, string filter) {
            var cursorStart = command.cursor == null ? "" : $"id < '{command.cursor}' AND ";
            var cursorDesc  = command.maxCount == null ? "" : " ORDER BY id DESC";
            string limit;
            if (command.maxCount != null) {
                limit       = $" LIMIT {command.maxCount}";
            } else {
                limit       = command.limit == null ? "" : $" LIMIT {command.limit}";
            }
            return $"SELECT id, data FROM {table} WHERE {cursorStart}{filter}{cursorDesc}{limit}";
        }
        
        public static void AppendValues(StringBuilder sb, List<JsonEntity> entities) {
            var isFirst = true;
            foreach (var entity in entities)
            {
                if (isFirst) {
                    isFirst = false;   
                } else {
                    sb.Append(',');
                }
                sb.Append("('");
                entity.key.AppendTo(sb);
                sb.Append("','");
                sb.Append(entity.value.AsString());
                sb.Append("')");
            }
        }
        
        public static void AppendKeys(StringBuilder sb, List<JsonKey> keys) {
            var isFirst = true;
            sb.Append('(');
            foreach (var key in keys)
            {
                if (isFirst) {
                    isFirst = false;   
                } else {
                    sb.Append(',');
                }
                sb.Append('\'');
                key.AppendTo(sb);
                sb.Append('\'');
            }
            sb.Append(')');
        }
    }
    
        
    public readonly struct SQLResult
    {
        public  readonly    object              value;
        public  readonly    TaskExecuteError    error;
        public              bool                Success => error == null;
        
        public SQLResult(object value) {
            this.value  = value;
            error       = null;
        }
        
        public SQLResult(TaskExecuteError error) {
            value       = null;
            this.error  = error;
        }
    }
}