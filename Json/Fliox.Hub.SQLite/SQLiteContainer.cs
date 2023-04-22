// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteContainer : EntityContainer
    {
        private  readonly   SQLiteDatabase      sqliteDB;
        private             bool                tableExists;
        public   override   bool                Pretty      { get; }
        
        internal SQLiteContainer(string name, SQLiteDatabase database, bool pretty)
            : base(name, database)
        {
            sqliteDB  = database;
            Pretty      = pretty;
        }

        private void EnsureContainerExists() {
            if (tableExists) {
                return;
            }
            tableExists = true;
            var sql = $"CREATE TABLE IF NOT EXISTS {name} (id TEXT PRIMARY KEY, data TEXT NOT NULL)";
            SQLiteUtils.Execute(sqliteDB.sqliteDB, sql, "CREATE TABLE");
        }
        
        public override Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            var sb = new StringBuilder();
            sb.Append("INSERT INTO "); sb.Append(name); sb.Append(" (id, data) VALUES");
            SQLiteUtils.AppendEntities(sb, command.entities);
            var sql = sb.ToString();
            SQLiteUtils.Execute(sqliteDB.sqliteDB, sql, "INSERT or UPDATE");
            return Task.FromResult(new CreateEntitiesResult());
        }

        public override Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            var sb = new StringBuilder();
            sb.Append("INSERT INTO "); sb.Append(name); sb.Append(" (id, data) VALUES\n");
            SQLiteUtils.AppendEntities(sb, command.entities);
            sb.Append("\nON CONFLICT(id) DO UPDATE SET data=excluded.data");
            var sql = sb.ToString();
            SQLiteUtils.Execute(sqliteDB.sqliteDB, sql, "INSERT or UPDATE");
            return Task.FromResult(new UpsertEntitiesResult());
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            throw new NotImplementedException();
        }
        
        public override Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            var filter = command.GetFilter().SQLiteFilter();
            var sql = $"SELECT id, data FROM {name} WHERE {filter}";
            var rc = raw.sqlite3_prepare_v3(sqliteDB.sqliteDB, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"SELECT - prepare error: {rc}");
            
            var values = new List<EntityValue>();
            while (true) {
                rc = raw.sqlite3_step(stmt);
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
            var result = new QueryEntitiesResult { entities = values.ToArray() }; 
            return Task.FromResult(result);
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            throw new NotImplementedException();
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            throw new NotImplementedException();
        }
    }
}

#endif