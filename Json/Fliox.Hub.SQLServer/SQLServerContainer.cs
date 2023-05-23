// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Microsoft.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;
using static Friflo.Json.Fliox.Hub.Host.Utils.SQLName;


// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public sealed class SQLServerContainer : EntityContainer, ISQLContainer
    {
        private  readonly   TableInfo           tableInfo;
        private             bool                tableExists;
        public   override   bool                Pretty      { get; }
        private   readonly  SQLServerDatabase   database;
        
        // [Maximum capacity specifications for SQL Server - SQL Server | Microsoft Learn]
        // https://learn.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver16
        // Bytes per primary key 900, used 255 same as in MySQL
        internal const string ColumnId     = ID   + " NVARCHAR(255)";
        internal const string ColumnData   = DATA + " NVARCHAR(max)";
        
        internal SQLServerContainer(string name, SQLServerDatabase database, bool pretty)
            : base(name, database)
        {
            tableInfo       = new TableInfo (database, name);
            Pretty          = pretty;
            this.database   = database;
        }
        
        public async Task<TaskExecuteError> EnsureContainerExists(SyncConnection connection) {
            if (tableExists) {
                return null;
            }
            var sql = $@"IF NOT EXISTS (
    SELECT * FROM sys.tables t JOIN sys.schemas s ON (t.schema_id = s.schema_id)
    WHERE s.name = 'dbo' AND t.name = '{name}')
CREATE TABLE dbo.{name} ({ColumnId} PRIMARY KEY, {ColumnData});";
            var result = await Execute(connection, sql).ConfigureAwait(false);
            if (result.Failed) {
                return result.error;
            }
            tableExists = true;
            await AddVirtualColumns(connection);
            return null;
        }
        
        public async Task AddVirtualColumns(SyncConnection connection) {
            using var cmd   = Command($"SELECT TOP 0 * FROM {name}", connection);
            var columnNames = await SQLUtils.GetColumnNames(cmd);
            foreach (var column in tableInfo.columns.Values) {
                if (column == tableInfo.keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                await AddVirtualColumn(connection, name, column);
            }
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (connection.Failed) {
                return new CreateEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new CreateEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            try {
                await database.CreateTableTypes().ConfigureAwait(false);
                using var cmd = CreateEntitiesCmd(connection, command.entities, name);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            } catch (SqlException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (connection.Failed) {
                return new UpsertEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            await database.CreateTableTypes().ConfigureAwait(false);
            using var cmd = UpsertEntitiesCmd(connection, command.entities, name);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (connection.Failed) {
                return new ReadEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new ReadEntitiesResult { Error = error };
            }
            await database.CreateTableTypes().ConfigureAwait(false);
            using var cmd = ReadEntitiesCmd(connection, command.ids, name);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            
            return await SQLUtils.ReadEntities(cmd, command).ConfigureAwait(false);
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (connection.Failed) {
                return new QueryEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new QueryEntitiesResult { Error = error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "(1=1)" : filter.SQLServerFilter();
            var sql     = SQLServerUtils.QueryEntities(command, name, where);
            try {
                using var cmd = Command(sql, connection);
                return await SQLUtils.QueryEntities(cmd, command, sql).ConfigureAwait(false);
            }
            catch (SqlException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (connection.Failed) {
                return new AggregateEntitiesResult { Error = connection.error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLServerFilter()}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                var result  = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new AggregateEntitiesResult { Error = result.error }; }
                return new AggregateEntitiesResult { value = (int)result.value };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (connection.Failed) {
                return new DeleteEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new DeleteEntitiesResult { Error = error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                var result = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new DeleteEntitiesResult { Error = result.error }; }
                return new DeleteEntitiesResult();    
            } else {
                await database.CreateTableTypes().ConfigureAwait(false);
                using var cmd = DeleteEntitiesCmd(connection, command.ids, name);
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