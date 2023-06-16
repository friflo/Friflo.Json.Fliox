// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseIndexFromEndExpression
// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    [Flags]
    public enum SQLEscape {
        Default     = 0,
        BackSlash   = 1,
        PrefixN     = 2,
    }
    
    public static class SQLName
    {
        public const string ID     = "json_id";
        public const string DATA   = "json_data";
    }

    public static class SQLUtils
    {
        public static string QueryEntitiesSQL(QueryEntities command, string table, string filter) {
            var cursorStart = command.cursor == null ? "" : $"{ID} < '{command.cursor}' AND ";
            var cursorDesc  = command.maxCount == null ? "" : $" ORDER BY {ID} DESC";
            string limit;
            if (command.maxCount != null) {
                limit       = $" LIMIT {command.maxCount}";
            } else {
                limit       = command.limit == null ? "" : $" LIMIT {command.limit}";
            }
            return $"SELECT {ID}, {DATA} FROM {table} WHERE {cursorStart}{filter}{cursorDesc}{limit}";
        }
        
        public static DataTable ToDataTable(List<JsonEntity> entities) {
            var table = new DataTable();
            table.Columns.Add(ID,   typeof(string));
            table.Columns.Add(DATA, typeof(string));
            var rows        = table.Rows;
            var rowValues   = new object[2]; 
            foreach (var entity in entities) {
                var key         = entity.key.AsString();
                var value       = entity.value.AsString();
                rowValues[0]    = key;
                rowValues[1]    = value;
                rows.Add(rowValues);
            }
            return table;
        }
        
        public static void AppendValuesSQL(StringBuilder sb, List<JsonEntity> entities, SQLEscape escape) {
            var escaped = new StringBuilder();
            var isFirst = true;
            foreach (var entity in entities)
            {
                if (isFirst) {
                    isFirst = false;   
                } else {
                    sb.Append(',');
                }
                sb.Append("('");
                entity.key.AppendTo(escaped);
                AppendEscaped(sb, escaped, escape);
                sb.Append("','");
                escaped.Append(entity.value.AsString());
                AppendEscaped(sb, escaped, escape);
                sb.Append("')");
            }
        }
        
        private static void AppendEscaped(StringBuilder sb, StringBuilder escaped, SQLEscape escape) {
            if ((escape & SQLEscape.BackSlash) != 0) {
                escaped.Replace("\\", "\\\\", 0, escaped.Length);
            }
            escaped.Replace("'", "''",    0, escaped.Length);
            sb.Append(escaped);
            escaped.Length = 0;
        }
        
        public static void AppendKeysSQL(StringBuilder sb, List<JsonKey> keys, SQLEscape escape) {
            var escaped = new StringBuilder();
            var isFirst = true;
            sb.Append('(');
            foreach (var key in keys)
            {
                if (isFirst) {
                    isFirst = false;   
                } else {
                    sb.Append(',');
                }
                if ((escape & SQLEscape.PrefixN) != 0) {
                    sb.Append('N');                    
                }
                sb.Append('\'');
                key.AppendTo(escaped);
                AppendEscaped(sb, escaped, escape);
                sb.Append('\'');
            }
            sb.Append(')');
        }
        
        // TODO remove 
        public static async Task<ReadEntitiesResult> ReadEntitiesAsync(DbCommand cmd, ReadEntities query) {
            using var reader= await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            return await ReadEntitiesAsync(reader, query).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Prefer using <see cref="ReadEntitiesSync"/> for SQL Server for performance.<br/>
        /// reading a single record - asynchronous: ~700 µs, synchronous: 100µs
        /// </summary>
        public static async Task<ReadEntitiesResult> ReadEntitiesAsync(DbDataReader reader, ReadEntities query) {
            var ids = query.ids;
            var rows        = new List<EntityValue>(ids.Count);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var id      = reader.GetString(0);
                var data    = reader.GetString(1);
                var key     = new JsonKey(id);
                var value   = new JsonValue(data);
                rows.Add(new EntityValue(key, value));
            }
            var entities = KeyValueUtils.EntityListToArray(rows, ids);
            return new ReadEntitiesResult { entities = entities };
        }
        
        public static ReadEntitiesResult ReadEntitiesSync(DbCommand cmd, ReadEntities query) {
            var ids = query.ids;
            using var reader= cmd.ExecuteReader();
            var rows        = new List<EntityValue>(ids.Count);
            while (reader.Read()) {
                var id      = reader.GetString(0);
                var data    = reader.GetString(1);
                var key     = new JsonKey(id);
                var value   = new JsonValue(data);
                rows.Add(new EntityValue(key, value));
            }
            var entities = KeyValueUtils.EntityListToArray(rows, ids);
            return new ReadEntitiesResult { entities = entities };
        }
        
        // TODO remove
        public static async Task<List<EntityValue>> QueryEntitiesAsync(DbCommand cmd) {
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            return await QueryEntitiesAsync(reader).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Prefer using <see cref="QueryEntitiesSync"/> for SQL Server for performance.<br/>
        /// E.g. reading two records - asynchronous: ~700 µs, synchronous: 100µs
        /// </summary>
        public static async Task<List<EntityValue>> QueryEntitiesAsync(DbDataReader reader) {
            var entities = new List<EntityValue>();
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var id      = reader.GetString(0);
                var data    = reader.GetString(1);
                var key     = new JsonKey(id);
                var value   = new JsonValue(data);
                entities.Add(new EntityValue(key, value));
            }
            return entities;
        }
        
        public static List<EntityValue> QueryEntitiesSync(DbCommand cmd) {
            using var reader = cmd.ExecuteReader();
            var entities = new List<EntityValue>();
            while (reader.Read()) {
                var id      = reader.GetString(0);
                var data    = reader.GetString(1);
                var key     = new JsonKey(id);
                var value   = new JsonValue(data);
                entities.Add(new EntityValue(key, value));
            }
            return entities;
        }
        
        public static QueryEntitiesResult CreateQueryEntitiesResult(List<EntityValue> entities, QueryEntities query, string sql) {
            var result = new QueryEntitiesResult { entities = entities.ToArray(), sql = sql };
            if (entities.Count >= query.maxCount) {
                result.cursor = entities[entities.Count - 1].key.AsString();
            }
            return result;
        }
        
        public static async Task<HashSet<string>> GetColumnNamesAsync(DbCommand cmd) {
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            return await GetColumnNamesAsync(reader);
        }
        
        public static async Task<HashSet<string>> GetColumnNamesAsync(DbDataReader reader) {
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                throw new InvalidOperationException("expect 0 rows");
            }
            var schema  = reader.GetSchemaTable();
            var columns = schema!.Columns;
            var column  = columns["ColumnName"];
            var rows    = schema.Rows;
            var result  = Helper.CreateHashSet<string>(rows.Count);
            for (int n = 0; n < rows.Count; n++) {
                var row     = rows[n];
                var name    = row.ItemArray[column.Ordinal];
                result.Add((string)name);
            }
            return result;
        }
        
        public static string Escape(string value) {
            return value.Replace("'", "''");
        }
        
        // https://www.sqlshack.com/introduction-to-sql-escape/
        public static string ToSqlString(
            string value,
            string concatStart,
            string concatDelimiter,
            string concatEnd,
            string charFunction,
            string literalStart = "'")
        {
            if (value == "") {
                return "''";
            }
            var sb = new StringBuilder();
            var parts   = new List<string>();
            foreach (var c in value) {
                if (c >= 32) {
                    if (sb.Length == 0) { sb.Append(literalStart); }
                    sb.Append(c);
                    continue;
                }
                if (sb.Length > 0) {
                    sb.Append('\'');
                    parts.Add(sb.ToString());
                    sb.Clear();
                }
                parts.Add($"{charFunction}({(int)c})");
            }
            if (sb.Length > 0) {
                sb.Append('\'');
                parts.Add(sb.ToString());
            }
            if (parts.Count == 1) {
                return parts[0]; 
            }
            sb.Clear();
            sb.Append(concatStart);
            sb.Append(parts[0]);
            for (int n = 1; n < parts.Count; n++) {
                sb.Append(concatDelimiter);
                sb.Append(parts[n]);
            }
            sb.Append(concatEnd);
            return sb.ToString();
        }
    }
    
        
    public readonly struct SQLResult
    {
        public  readonly    object              value;
        public  readonly    TaskExecuteError    error;
        public              bool                Failed => error != null;
        
        public SQLResult(object value) {
            this.value  = value;
            error       = null;
        }
        
        public SQLResult(Exception e) {
            value       = null;
            error       = new TaskExecuteError(e.Message);
        }
    }
}