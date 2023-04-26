// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
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
        
        internal static bool Execute(sqlite3 db, string sql, out TaskExecuteError error) {
            var rc = raw.sqlite3_prepare_v3(db, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) {
                var msg = $"prepare failed. sql: ${sql}, error: {rc}";
                error = new TaskExecuteError(TaskErrorType.DatabaseError, msg);
                return false;
            }
            rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_DONE) {
                var msg = $"step failed. sql: ${sql}, error: {rc}";
                error = new TaskExecuteError(TaskErrorType.DatabaseError, msg);
                return false;
            }
            raw.sqlite3_finalize(stmt);
            error = null;
            return true;
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
        
        internal static bool ReadValues(sqlite3_stmt stmt, List<EntityValue> values, MemoryBuffer buffer, out TaskExecuteError error)
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
                    error = new TaskExecuteError("step failed");
                    return false;
                }
            }
            error = null;
            return true;
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
        
        private static int BindKey(sqlite3_stmt stmt, in JsonKey key, ref Bytes bytes) {
            var encoding = key.GetEncoding();
            switch (encoding) {
                case JsonKeyEncoding.LONG:
                    return raw.sqlite3_bind_int64(stmt, 1, key.AsLong());
                case JsonKeyEncoding.STRING:
                    return raw.sqlite3_bind_text (stmt, 1, key.AsString());
                case JsonKeyEncoding.STRING_SHORT:
                case JsonKeyEncoding.GUID:
                    key.ToBytes(ref bytes);
                    return raw.sqlite3_bind_text (stmt, 1, bytes.AsSpan());
                default:
                    throw new InvalidOperationException("unhandled case");
            }
        }

        // [c - Improve INSERT-per-second performance of SQLite - Stack Overflow]
        // https://stackoverflow.com/questions/1711631/improve-insert-per-second-performance-of-sqlite
        internal static bool AppendValues(sqlite3_stmt stmt, List<JsonEntity> entities, out TaskExecuteError error)
        {
            var bytes = new Bytes(36);
            foreach (var entity in entities) {
                var key = entity.key;
                var rc  = BindKey(stmt, key, ref bytes);
                if (rc != raw.SQLITE_OK) {
                    error = new TaskExecuteError($"bind key failed. error: {rc}, key: {key}");
                    return false;
                }
                rc = raw.sqlite3_bind_text(stmt, 2, entity.value.AsReadOnlySpan());
                if (rc != raw.SQLITE_OK) {
                    error = new TaskExecuteError($"bind value failed. error: {rc}, key: {key}");
                    return false;
                }
                rc = raw.sqlite3_step(stmt);
                if (rc != raw.SQLITE_DONE) {
                    error = new TaskExecuteError($"step failed. error: {rc}, key: {key}");
                    return false;
                }
                raw.sqlite3_reset(stmt);
            }
            error = null;
            return true;
        }
        
        internal static bool AppendKeys(sqlite3_stmt stmt, List<JsonKey> keys, out TaskExecuteError error)
        {
            var bytes = new Bytes(36);
            foreach (var key in keys) {
                var rc  = BindKey(stmt, key, ref bytes);
                if (rc != raw.SQLITE_OK) {
                    error = new TaskExecuteError($"bind key failed. error: {rc}, key: {key}");
                    return false;
                }
                rc = raw.sqlite3_step(stmt);
                if (rc != raw.SQLITE_DONE) {
                    error = new TaskExecuteError($"step failed. error: {rc}, key: {key}");
                    return false;
                }
                raw.sqlite3_reset(stmt);
            }
            error = null;
            return true;
        }
        
        internal static bool Prepare(sqlite3 db, string sql, out sqlite3_stmt stmt, out TaskExecuteError error) {
            var rc  = raw.sqlite3_prepare_v3(db, sql, 0, out stmt);
            if (rc == raw.SQLITE_OK) {
                error = null;
                return true;
            }
            var msg = $"prepare failed. sql: {sql}, error: {rc}";
            error   = new TaskExecuteError(TaskErrorType.DatabaseError, msg);
            return false;
        }
        
        internal static bool Exec(sqlite3 db, string sql, out TaskExecuteError error) {
            var rc = raw.sqlite3_exec(db, sql, null, 0, out var errMsg);
            if (rc == raw.SQLITE_OK) {
                error = null;
                return true;
            }
            error = new TaskExecuteError(TaskErrorType.DatabaseError, $"exec failed. sql: {sql}, error: {errMsg}");
            return false;
        }
    }
}

#endif