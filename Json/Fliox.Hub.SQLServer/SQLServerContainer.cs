// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Microsoft.Data.SqlClient;


// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public sealed class SQLServerContainer : EntityContainer
    {
        private             bool            tableExists;
        public   override   bool            Pretty      { get; }
        
        internal SQLServerContainer(string name, SQLServerDatabase database, bool pretty)
            : base(name, database)
        {
            Pretty          = pretty;
        }

        private async Task<TaskExecuteError> EnsureContainerExists(SyncContext syncContext) {
            if (tableExists) {
                return null;
            }
            var sql = $@"IF NOT EXISTS (
    SELECT * FROM sys.tables t JOIN sys.schemas s ON (t.schema_id = s.schema_id)
    WHERE s.name = 'dbo' AND t.name = '{name}')
CREATE TABLE dbo.{name} (id VARCHAR(128) PRIMARY KEY, data VARCHAR(max));";
            var connection = await syncContext.GetConnection();
            if (!connection.Success) { return connection.error; }
            var result = await SQLServerUtils.Execute(connection.instance as SqlConnection, sql).ConfigureAwait(false);
            if (!result.Success) {
                return result.error;
            }
            tableExists = true;
            return null;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists(syncContext).ConfigureAwait(false);
            if (error != null) {
                return new CreateEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {name} (id,data) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities);
            var connection = await syncContext.GetConnection();
            if (!connection.Success) { return new CreateEntitiesResult { Error = connection.error }; }
            using var cmd = new SqlCommand(sql.ToString(), connection.instance as SqlConnection);
            try {
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            } catch (SqlException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists(syncContext).ConfigureAwait(false);
            if (error != null) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append(
$@"MERGE {name} AS target
USING (VALUES");
            SQLUtils.AppendValuesSQL(sql, command.entities);
            sql.Append(
@") AS source (id, data)
ON source.id = target.id
WHEN MATCHED THEN
    UPDATE SET target.data = source.data
WHEN NOT MATCHED THEN
    INSERT (id, data)
    VALUES (id, data);");
            var connection = await syncContext.GetConnection();
            if (!connection.Success) { return new UpsertEntitiesResult { Error = connection.error }; }
            using var cmd = new SqlCommand(sql.ToString(), connection.instance as SqlConnection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists(syncContext).ConfigureAwait(false);
            if (error != null) {
                return new ReadEntitiesResult { Error = error };
            }
            var ids = command.ids;
            var sql = new StringBuilder();
            sql.Append($"SELECT id, data FROM {name} WHERE id in\n");
            SQLUtils.AppendKeysSQL(sql, ids);
            var connection = await syncContext.GetConnection();
            if (!connection.Success) { return new ReadEntitiesResult { Error = connection.error }; }
            using var cmd   = new SqlCommand(sql.ToString(), connection.instance as SqlConnection);
            return await SQLUtils.ReadEntities(cmd, command).ConfigureAwait(false);
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists(syncContext).ConfigureAwait(false);
            if (error != null) {
                return new QueryEntitiesResult { Error = error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "(1=1)" : filter.SQLServerFilter();
            var sql     = SQLServerUtils.QueryEntities(command, name, where);
            try {
                var connection = await syncContext.GetConnection();
                if (!connection.Success) { return new QueryEntitiesResult { Error = connection.error }; }
                using var cmd = new SqlCommand(sql, connection.instance as SqlConnection);
                return await SQLUtils.QueryEntities(cmd, command, sql).ConfigureAwait(false);
            }
            catch (SqlException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLServerFilter()}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                var connection = await syncContext.GetConnection();
                if (!connection.Success) { return new AggregateEntitiesResult { Error = connection.error }; }
                var result  = await SQLServerUtils.Execute(connection.instance as SqlConnection, sql).ConfigureAwait(false);
                if (!result.Success) { return new AggregateEntitiesResult { Error = result.error }; }
                return new AggregateEntitiesResult { value = (int)result.value };
            }
            throw new NotImplementedException();
        }
        
       
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var error = await EnsureContainerExists(syncContext).ConfigureAwait(false);
            if (error != null) {
                return new DeleteEntitiesResult { Error = error };
            }
            var connection = await syncContext.GetConnection();
            if (!connection.Success) { return new DeleteEntitiesResult { Error = connection.error }; }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                var result = await SQLServerUtils.Execute(connection.instance as SqlConnection, sql).ConfigureAwait(false);
                if (!result.Success) { return new DeleteEntitiesResult { Error = result.error }; }
                return new DeleteEntitiesResult();    
            } else {
                var sql = new StringBuilder();
                sql.Append($"DELETE FROM  {name} WHERE id in\n");
                
                SQLUtils.AppendKeysSQL(sql, command.ids);
                using var cmd = new SqlCommand(sql.ToString(), connection.instance as SqlConnection);
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