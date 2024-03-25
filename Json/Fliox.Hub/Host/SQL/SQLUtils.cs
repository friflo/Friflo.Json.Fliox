// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
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
        HasBool     = 4
    }
    
    public static class SQLName
    {
        public const string ID     = "json_id";
        public const string DATA   = "json_data";
    }

    public static class SQLUtils
    {
        public static string QueryEntitiesSQL(QueryEntities command, string table, string filter, TableInfo tableInfo) {
            var tableType   = tableInfo.tableType;
            var id          = tableType == TableType.Relational ? tableInfo.keyColumn.name : ID;
            var cursorStart = command.cursor == null ? "" : $"{id} < '{command.cursor}' AND ";
            var cursorDesc  = command.maxCount == null ? "" : $" ORDER BY {tableInfo.colStart}{id}{tableInfo.colEnd} DESC";
            string limit;
            if (command.maxCount != null) {
                limit       = $" LIMIT {command.maxCount}";
            } else {
                limit       = command.limit == null ? "" : $" LIMIT {command.limit}";
            }
            if (tableType == TableType.Relational) {
                var sql = new StringBuilder();
                sql.Append("SELECT ");
                SQLTable.AppendColumnNames(sql, tableInfo);
                sql.Append($" FROM {table} WHERE {cursorStart}{filter}{cursorDesc}{limit}");
                return sql.ToString();
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
                    sb.Append(",\n");
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
        
        public static void AppendKeysSQL(StringBuilder sb, ListOne<JsonKey> keys, SQLEscape escape) {
            var escaped = new StringBuilder();
            var isFirst = true;
            sb.Append('(');
            foreach (var key in keys.GetReadOnlySpan())
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
        
        // TODO consolidate with AppendKeysSQL() above
        public static StringBuilder AppendKeysSQL2(StringBuilder sb, ListOne<JsonKey> keys, SQLEscape escape) {
            var escaped = new StringBuilder();
            var isFirst = true;
            foreach (var key in keys.GetReadOnlySpan())
            {
                if (isFirst) {
                    isFirst = false;   
                } else {
                    sb.Append(',');
                }
                if (key.IsLong()) {
                    key.AppendTo(sb);
                    continue;
                }
                if ((escape & SQLEscape.PrefixN) != 0) {
                    sb.Append('N');                    
                }
                sb.Append('\'');
                key.AppendTo(escaped);
                AppendEscaped(sb, escaped, escape);
                sb.Append('\'');
            }
            return sb;
        }
        
        /// <summary>
        /// Prefer using <see cref="ReadJsonColumnSync"/> for SQL Server for performance.<br/>
        /// reading a single record - asynchronous: ~700 µs, synchronous: 100µs
        /// </summary>
        public static async Task<ReadEntitiesResult> ReadJsonColumnAsync(DbDataReader reader, ReadEntities query) {
            var ids = query.ids;
            var rows        = new List<EntityValue>(ids.Count);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                var id      = reader.GetString(0);
                var data    = reader.GetString(1);
                var key     = new JsonKey(id);
                var value   = new JsonValue(data);
                rows.Add(new EntityValue(key, value));
            }
            return new ReadEntitiesResult { entities = new Entities(rows) };
        }
        
        public static ReadEntitiesResult ReadJsonColumnSync(DbDataReader reader, ReadEntities query) {
            var ids = query.ids;
            var rows        = new List<EntityValue>(ids.Count);
            while (reader.Read()) {
                var id      = reader.GetString(0);
                var data    = reader.GetString(1);
                var key     = new JsonKey(id);
                var value   = new JsonValue(data);
                rows.Add(new EntityValue(key, value));
            }
            return new ReadEntitiesResult { entities = new Entities(rows) };
        }
        
        /// <summary>
        /// Async version of <see cref="QueryJsonColumnSync"/><br/>
        /// Prefer using <see cref="QueryJsonColumnSync"/> for SQL Server for performance.<br/>
        /// E.g. reading two records - asynchronous: ~700 µs, synchronous: 100µs
        /// </summary>
        public static async Task<List<EntityValue>> QueryJsonColumnAsync(DbDataReader reader) {
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
        
        /// <summary>sync version of <see cref="QueryJsonColumnAsync"/></summary>
        public static List<EntityValue> QueryJsonColumnSync(DbDataReader reader) {
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
            var result = new QueryEntitiesResult { entities = new Entities(entities), sql = sql };
            if (entities.Count >= query.maxCount) {
                result.cursor = entities[entities.Count - 1].key.AsString();
            }
            return result;
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
    
    public readonly struct SQLError
    {
        public  readonly    string  message;
        
        public SQLError(string message) {
            this.message = message;
        }
    }

    public readonly struct SQLResult
    {
        public  readonly    object  value;
        public  readonly    string  error;
        public              bool    Failed => error != null;
        
        public  TaskExecuteError TaskError() {
            return new TaskExecuteError (error);
        }
        
        public static SQLResult Success(object value) {
            return new SQLResult(value);
        }
        
        public static SQLResult CreateError(string message) {
            return new SQLResult(message);
        }
        
        public static SQLResult CreateError(Exception e) {
            return new SQLResult(e.Message);
        }

        private SQLResult(object value) {
            this.value  = value;
            error       = null;
        }
        
        private SQLResult(string message) {
            value       = null;
            error       = message;
        }
    }
}