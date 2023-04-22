// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal static class SQLiteUtils
    {
        internal static void Execute(sqlite3 db, string sql, string description) {
            var rc = raw.sqlite3_prepare_v3(db, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"{description} - prepare error: {rc}");
            rc = raw.sqlite3_step(stmt);
            if (rc != raw.SQLITE_DONE) throw new InvalidOperationException($"{description} - step error: {rc}");
            raw.sqlite3_finalize(stmt);
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