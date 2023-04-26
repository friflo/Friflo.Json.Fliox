// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Utils;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal static class SQLiteUtils
    {
        internal static string GetVersion(sqlite3 db) {
            var sql = "select sqlite_version()";
            var rc  = raw.sqlite3_prepare_v3(db, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"sqlite_version() - prepare error: {rc}");
            rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_ROW) throw new InvalidOperationException($"sqlite_version() - step error: {rc}");
            var text    = raw.sqlite3_column_text(stmt, 0);
            var version = text.utf8_to_string();
            return version;
        }
        
        internal static void Execute(sqlite3 db, string sql, string description) {
            var rc = raw.sqlite3_prepare_v3(db, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"{description} - prepare error: {rc}");
            rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_DONE) throw new InvalidOperationException($"{description} - step error: {rc}");
            raw.sqlite3_finalize(stmt);
        }
        
        internal static void AppendIds(StringBuilder sb, List<JsonKey> ids) {
            bool isFirst = true;
            foreach (var id in ids) {
                if (isFirst) {
                    isFirst = false;
                } else {
                    sb.Append(',');
                }
                if (id.IsLong()) {
                    id.AppendTo(sb);
                } else {
                    sb.Append('\'');
                    id.AppendTo(sb);
                    sb.Append('\'');
                }
            }
        }
        
        internal static void ReadValues(sqlite3_stmt stmt, List<EntityValue> values, MemoryBuffer buffer)
        {
            while (true) {
                var rc = raw.sqlite3_step(stmt);
                if (rc == raw.SQLITE_ROW) {
                    var id      = raw.sqlite3_column_blob(stmt, 0);
                    var data    = raw.sqlite3_column_blob(stmt, 1);
                    var key     = new JsonKey(id, default);
                    var value   = buffer.Add(data);
                    values.Add(new EntityValue(key, value));
                } else if (rc == raw.SQLITE_DONE) {
                    break;
                } else {
                    throw new InvalidOperationException($"SELECT - step error: {rc}");
                }
            }
        }
        
        // requires insert statement like: "INSERT INTO <table> (id, data) VALUES"
        internal static void AppendEntities(StringBuilder sb, List<JsonEntity> entities)
        {
            bool isFirst = true; 
            foreach (var entity in entities) {
                if (isFirst) {
                    isFirst = false;
                } else {
                    sb.Append(",");
                }
                sb.Append("('");
                sb.Append(entity.key.AsString());
                sb.Append("','");
                sb.Append(entity.value.AsString());
                sb.Append("')");
            }
        }

        // [c - Improve INSERT-per-second performance of SQLite - Stack Overflow]
        // https://stackoverflow.com/questions/1711631/improve-insert-per-second-performance-of-sqlite
        internal static void AppendValues(sqlite3_stmt stmt, List<JsonEntity> entities)
        {
            var bytes = new Bytes(36);
            foreach (var entity in entities) {
                var key         = entity.key;
                var encoding    = key.GetEncoding();
                switch (encoding) {
                    case JsonKeyEncoding.LONG:
                        raw.sqlite3_bind_int64(stmt, 1, key.AsLong());
                        break;
                    case JsonKeyEncoding.STRING:
                        raw.sqlite3_bind_text (stmt, 1, key.AsString());
                        break;
                    case JsonKeyEncoding.STRING_SHORT:
                    case JsonKeyEncoding.GUID:
                        key.ToBytes(ref bytes);
                        raw.sqlite3_bind_text (stmt, 1, bytes.AsSpan());
                        break;
                    default:
                        throw new InvalidOperationException("unhandled case");
                }
                raw.sqlite3_bind_blob(stmt, 2, entity.value.AsReadOnlySpan());
                
                var rc = raw.sqlite3_step(stmt);
                if (rc != raw.SQLITE_DONE) throw new InvalidOperationException($"AppendValues - step error: {rc}");
                raw.sqlite3_reset(stmt);
            }
        }
    }
}

#endif