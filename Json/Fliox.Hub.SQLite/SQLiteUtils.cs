// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Utils;
using SQLitePCL;
using static Friflo.Json.Fliox.Hub.Host.Utils.SQLName;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal static class SQLiteUtils
    {
        internal static string GetVersion(sqlite3 db) {
            var sql     = "select sqlite_version()";
            var rc      = raw.sqlite3_prepare_v3(db, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"sqlite_version() - prepare error: {rc}");
            rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_ROW) throw new InvalidOperationException($"sqlite_version() - step error: {rc}");
            var text    = raw.sqlite3_column_text(stmt, 0);
            var version = text.utf8_to_string();
            return version;
        }
        
        private static bool Success(out TaskExecuteError error) {
            error = null;
            return true;
        }
        
        private static bool Error(string msg, out TaskExecuteError error) {
            error = new TaskExecuteError(TaskErrorType.DatabaseError, msg);
            return false;
        }
        
        internal static bool Execute(sqlite3 db, string sql, out TaskExecuteError error) {
            var rc = raw.sqlite3_prepare_v3(db, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) {
                return Error($"prepare failed. sql: ${sql}, error: {rc}", out error);
            }
            rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_DONE) {
                return Error($"step failed. sql: ${sql}, error: {rc}", out error);
            }
            raw.sqlite3_finalize(stmt);
            return Success(out error);
        }
        
        private static string GetSqlType(StandardTypeId typeId) {
            switch (typeId) {
                case StandardTypeId.Uint8:
                case StandardTypeId.Int16:
                case StandardTypeId.Int32:
                case StandardTypeId.Int64:      return "integer";
                case StandardTypeId.Float:
                case StandardTypeId.Double:     return "real";
                case StandardTypeId.Boolean:    return "text";
                case StandardTypeId.DateTime:
                case StandardTypeId.Guid:
                case StandardTypeId.BigInteger:
                case StandardTypeId.String:
                case StandardTypeId.Enum:       return "text";
            }
            throw new NotSupportedException($"column type: {typeId}");
        }
        
        internal static HashSet<string> GetColumnNames(sqlite3 db, string table) {
            var sql = $"SELECT * FROM {table} LIMIT 0";
            Prepare(db, sql, out var stmt, out var error);
            var count   = raw.sqlite3_column_count(stmt);
            var result = Helper.CreateHashSet<string>(count);
            for (int n = 0; n < count; n++) {
                var name = raw.sqlite3_column_name(stmt, n).utf8_to_string();
                result.Add(name); 
            }
            raw.sqlite3_finalize(stmt);
            return result;
        }
        
        internal static void AddVirtualColumn(sqlite3 db, string table, ColumnInfo column) {
            var type = GetSqlType(column.typeId);
            var sql =
$@"ALTER TABLE {table}
ADD COLUMN {column.name} {type}
AS (json_extract({DATA}, '$.{column.name}'));";
            Execute(db, sql, out _);
        }
        
        internal static bool ReadValues(
            sqlite3_stmt            stmt,
            int?                    maxCount,                     
            List<EntityValue>       values,
            MemoryBuffer            buffer,
            out TaskExecuteError    error)
        {
            int count = 0;
            while (true) {
                var rc = raw.sqlite3_step(stmt);
                if (rc == raw.SQLITE_ROW) {
                    var id      = raw.sqlite3_column_blob(stmt, 0);
                    var data    = raw.sqlite3_column_blob(stmt, 1);
                    var key     = new JsonKey(id, default);
                    var value   = buffer.Add(data);
                    values.Add(new EntityValue(key, value));
                    count++;
                    if (maxCount != null && count >= maxCount) {
                        return Success(out error);
                    }
                } else if (rc == raw.SQLITE_DONE) {
                    break;
                } else {
                    return Error("step failed", out error);
                }
            }
            return Success(out error);
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
                    return Error($"bind key failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_bind_text(stmt, 2, entity.value.AsReadOnlySpan());
                if (rc != raw.SQLITE_OK) {
                    return Error($"bind value failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_step(stmt);
                if (rc != raw.SQLITE_DONE) {
                    return Error($"step failed. error: {rc}, key: {key}", out error);
                }
                raw.sqlite3_reset(stmt);
            }
            return Success(out error);
        }
        
        internal static bool ReadById(
            sqlite3_stmt            stmt,
            List<JsonKey>           keys,
            List<EntityValue>       values,
            MemoryBuffer            buffer,
            out TaskExecuteError    error)
        {
            var bytes = new Bytes(36);
            foreach (var key in keys) {
                var rc  = BindKey(stmt, key, ref bytes);
                if (rc != raw.SQLITE_OK) {
                    return Error($"bind key failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_step(stmt);
                switch (rc) {
                    case raw.SQLITE_DONE: 
                        values.Add(new EntityValue(key));
                        break;
                    case raw.SQLITE_ROW:
                        var data    = raw.sqlite3_column_blob(stmt, 1);
                        var value   = buffer.Add(data);
                        values.Add(new EntityValue(key, value));
                        break;
                    default:
                        return Error($"step failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_reset(stmt);
                if (rc != raw.SQLITE_OK) {
                    return Error($"reset failed. error: {rc}, key: {key}", out error);
                }
            }
            return Success(out error);
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
                    return Error($"step failed. error: {rc}, key: {key}", out error);
                }
                raw.sqlite3_reset(stmt);
            }
            return Success(out error);
        }
        
        internal static bool Prepare(sqlite3 db, string sql, out sqlite3_stmt stmt, out TaskExecuteError error) {
            var rc  = raw.sqlite3_prepare_v3(db, sql, 0, out stmt);
            if (rc == raw.SQLITE_OK) {
                return Success(out error);
            }
            return Error($"prepare failed. sql: {sql}, error: {rc}", out error);
        }
        
        internal static bool Exec(sqlite3 db, string sql, out TaskExecuteError error) {
            var rc = raw.sqlite3_exec(db, sql, null, 0, out var errMsg);
            if (rc == raw.SQLITE_OK) {
                return Success(out error);
            }
            return Error($"exec failed. sql: {sql}, error: {errMsg}", out error);
        }
        
        /// <summary>
        /// BEGIN TRANSACTION.<br/>
        /// Need to be called in a using statement to END TRANSACTION by calling
        /// <see cref="TransactionScope.Dispose"/> when leaving the scope.
        /// </summary>
        internal static TransactionScope Transaction(sqlite3 db, out TaskExecuteError error) {
            if (!Exec(db, "BEGIN TRANSACTION", out error)) {
                return default;
            }
            return new TransactionScope(db);
        }
        
        internal readonly struct TransactionScope : IDisposable
        {
            private readonly sqlite3 db;
            
            internal TransactionScope(sqlite3 db) {
                this.db = db;
            }

            public void Dispose() {
                if (db == null) {
                    return;
                }
                Exec(db, "End TRANSACTION", out _);
            }
        }
       
    }
}

#endif