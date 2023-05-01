// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using Npgsql;


namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public sealed class PostgreSQLContainer : EntityContainer
    {
        private             bool                tableExists;
        public   override   bool                Pretty      { get; }
        private  readonly   PostgreSQLDatabase  database;
        private  readonly   TypeDef             entityType;
        
        internal PostgreSQLContainer(string name, PostgreSQLDatabase database, bool pretty)
            : base(name, database)
        {
            Pretty          = pretty;
            this.database   = database;
            var types       = database.Schema.typeSchema.GetEntityTypes();
            entityType      = types[name];
        }

        private async Task<TaskExecuteError> EnsureContainerExists() {
            if (tableExists) {
                return null;
            }
            var sql = $"CREATE TABLE if not exists {name} (id VARCHAR(128) PRIMARY KEY, data JSONB);";
            var result = await PostgreSQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
            if (result.error != null) {
                return result.error;
            }
            tableExists = true;
            return null;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new CreateEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {name} (id,data) VALUES\n");
            PostgreSQLUtils.AppendValues(sql, command.entities);
            using var cmd = new NpgsqlCommand(sql.ToString(), database.connection);
            try {
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            } catch (Exception e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {name} (id,data) VALUES\n");
            PostgreSQLUtils.AppendValues(sql, command.entities);
            sql.Append("\nON CONFLICT(id) DO UPDATE SET data = excluded.data;");
            using var cmd = new NpgsqlCommand(sql.ToString(), database.connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new ReadEntitiesResult { Error = error };
            }
            var ids = command.ids;
            var sql = new StringBuilder();
            sql.Append($"SELECT id, data FROM {name} WHERE id in\n");
            PostgreSQLUtils.AppendKeys(sql, ids);
            using var cmd   = new NpgsqlCommand(sql.ToString(), database.connection);
            using var reader= await cmd.ExecuteReaderAsync().ConfigureAwait(false);
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

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {

            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new QueryEntitiesResult { Error = error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.PostgresFilter(entityType);
            var sql     = SQLUtils.QueryEntities(command, name, where);
            using var cmd    = new NpgsqlCommand(sql, database.connection);
            try {
                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                var entities = new List<EntityValue>();
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var id      = reader.GetString(0);
                    var data    = reader.GetString(1);
                    var key     = new JsonKey(id);
                    var value   = new JsonValue(data);
                    entities.Add(new EntityValue(key, value));
                }
                var result = new QueryEntitiesResult { entities = entities.ToArray() };
                if (entities.Count >= command.maxCount) {
                    result.cursor = entities[entities.Count - 1].key.AsString();
                }
                return result;
            } catch (PostgresException e) {
                var msg = $"PG error: {e.MessageText}, SQL: \n{sql}";
                return new QueryEntitiesResult { Error = new TaskExecuteError(msg) };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.PostgresFilter(entityType)}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                var result  = await PostgreSQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            throw new NotImplementedException();
        }
        
       
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists().ConfigureAwait(false);
            if (error != null) {
                return new DeleteEntitiesResult { Error = error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                await PostgreSQLUtils.Execute(database.connection, sql).ConfigureAwait(false);
                return new DeleteEntitiesResult();    
            } else {
                var sql = new StringBuilder();
                sql.Append($"DELETE FROM  {name} WHERE id in\n");
                
                PostgreSQLUtils.AppendKeys(sql, command.ids);
                using var cmd = new NpgsqlCommand(sql.ToString(), database.connection);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                return new DeleteEntitiesResult();
            }
        }
        
        private static TaskExecuteError DatabaseError(string message) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, message);
        }
    }
}

#endif