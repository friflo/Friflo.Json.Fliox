// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal static class SQLiteUtils
    {
        internal static string GetVersion(sqlite3 db) {
            var sql = "select sqlite_version()";
            var rc  = raw.sqlite3_prepare_v3(db, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"sqlite_version() - prepare error: {rc}");
            var values = new List<EntityValue>();
            ReadValues(stmt, values);
            var version = values[0].key.AsString();
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
        
        internal static void ReadValues(sqlite3_stmt stmt, List<EntityValue> values) {
            while (true) {
                var rc = raw.sqlite3_step(stmt);
                if (rc == raw.SQLITE_ROW) {
                    var id      = raw.sqlite3_column_text(stmt, 0);
                    var data    = raw.sqlite3_column_text(stmt, 1);
                    var idStr   = id.utf8_to_string();
                    var dataStr = data.utf8_to_string();
                    var entity = new EntityValue(new JsonKey(idStr), new JsonValue(dataStr));
                    values.Add(entity);
                } else if (rc == raw.SQLITE_DONE) {
                    break;
                } else {
                    throw new InvalidOperationException($"SELECT - step error: {rc}");
                }
            }
        }
        
        internal static void AppendEntities(StringBuilder sb, List<JsonEntity> entities) {
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
    }
}

#endif