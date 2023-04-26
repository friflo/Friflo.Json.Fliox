// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

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
            var result = CreateEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override CreateEntitiesResult CreateEntities(CreateEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "BEGIN TRANSACTION", out var error)) {
                return new CreateEntitiesResult { Error = error };
            }
            var sql = $@"INSERT INTO {name} VALUES(?,?)";
            var rc = raw.sqlite3_prepare_v2(sqliteDB.sqliteDB, sql, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"UPSERT - prepare error: {rc}");
            SQLiteUtils.AppendValues(stmt, command.entities);
            raw.sqlite3_finalize(stmt);
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "END TRANSACTION", out error)) {
                return new CreateEntitiesResult { Error = error };
            }
            return new CreateEntitiesResult();
        }
        
        public override Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var result = UpsertEntities(command, syncContext);
            return Task.FromResult(result);
        }

        public override UpsertEntitiesResult UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "BEGIN TRANSACTION", out var error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            var sql = $@"INSERT INTO {name} VALUES(?,?) ON CONFLICT(id) DO UPDATE SET data=excluded.data";
            var rc = raw.sqlite3_prepare_v2(sqliteDB.sqliteDB, sql, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"UPSERT - prepare error: {rc}");
            
            SQLiteUtils.AppendValues(stmt, command.entities);
            raw.sqlite3_finalize(stmt);
            
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "END TRANSACTION", out error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            return new UpsertEntitiesResult();
        }
        
        public override Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var result = ReadEntities(command, syncContext);
            return Task.FromResult(result);
        }

        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            var sb = new StringBuilder();
            SQLiteUtils.AppendIds(sb, command.ids);
            var ids = sb.ToString();
            var sql = $"SELECT id, data FROM {name} WHERE id in ({ids})";
            var rc  = raw.sqlite3_prepare_v3(sqliteDB.sqliteDB, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"SELECT - prepare error: {rc}");
            var values = new List<EntityValue>();
            SQLiteUtils.ReadValues(stmt, values, syncContext.MemoryBuffer);
            return new ReadEntitiesResult { entities = values.ToArray() };
        }
        
        public override Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var result = QueryEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            var filter = command.GetFilter().SQLiteFilter();
            var sql = $"SELECT id, data FROM {name} WHERE {filter}";
            var rc = raw.sqlite3_prepare_v3(sqliteDB.sqliteDB, sql, 0, out var stmt);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"SELECT - prepare error: {rc}");
            
            var values = new List<EntityValue>();
            SQLiteUtils.ReadValues(stmt, values, syncContext.MemoryBuffer);
            return new QueryEntitiesResult { entities = values.ToArray() }; 
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var result = AggregateEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        private AggregateEntitiesResult AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            throw new NotImplementedException();
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var result = DeleteEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override DeleteEntitiesResult DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            EnsureContainerExists();
            throw new NotImplementedException();
        }
    }
}

#endif