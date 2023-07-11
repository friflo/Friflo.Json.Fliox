// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using System.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;


// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal sealed class SQLServerContainer : EntityContainer, ISQLTable
    {
        private  readonly   TableInfo           tableInfo;
        public   override   bool                Pretty      { get; }
        private  readonly   SQLServerDatabase   database;
        
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
        
        public async Task<TaskExecuteError> InitTable(ISyncConnection connection) {
            var sql =
$@"IF NOT EXISTS (
    SELECT * FROM sys.tables t JOIN sys.schemas s ON (t.schema_id = s.schema_id)
    WHERE s.name = 'dbo' AND t.name = '{name}')
CREATE TABLE dbo.{name} ({ColumnId} PRIMARY KEY, {ColumnData});";
            var result = await Execute((SyncConnection)connection, sql).ConfigureAwait(false);
            if (result.Failed) {
                    return result.error;
            }
            var error = await AddVirtualColumns(connection).ConfigureAwait(false);
            if (error != null) {
                return error;
            }
            return null;
        }
        
        private async Task<HashSet<string>> GetColumnNamesAsync(SyncConnection connection) {
            using var reader = await connection.ExecuteReaderAsync($"SELECT TOP 0 * FROM {name}").ConfigureAwait(false);
            return await SQLUtils.GetColumnNamesAsync(reader).ConfigureAwait(false);
        }
        
        public async Task<TaskExecuteError> AddVirtualColumns(ISyncConnection syncConnection) {
            var connection  = (SyncConnection)syncConnection;
            var columnNames = await GetColumnNamesAsync (connection).ConfigureAwait(false);
            foreach (var column in tableInfo.columns) {
                if (column == tableInfo.keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                var result = await AddVirtualColumn(connection, name, column).ConfigureAwait(false);
                if (result.Failed) {
                    return result.error;
                }
            }
            return null;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            try {
                await database.CreateTableTypes().ConfigureAwait(false);
                await CreateEntitiesCmdAsync(connection, command.entities, name).ConfigureAwait(false);
            } catch (SqlException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            try {
                await database.CreateTableTypes().ConfigureAwait(false);
                await UpsertEntitiesCmdAsync(connection, command.entities, name).ConfigureAwait(false);
            } catch (SqlException e) {
                return new UpsertEntitiesResult { Error = new TaskExecuteError(e.Message) };
            }
            return new UpsertEntitiesResult();
        }
        
        // ReSharper disable once ConvertToConstant.Local
        /// <summary>
        /// Using asynchronous execution for SQL Server is significant slower.<br/>
        /// <see cref="DbCommand.ExecuteReaderAsync()"/> ~7x slower than <see cref="DbCommand.ExecuteReader()"/>.
        /// </summary>
        private static readonly bool ExecuteAsync = false;

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (ExecuteAsync) {
                    await database.CreateTableTypes().ConfigureAwait(false);
                    using var reader = await ReadEntitiesCmd(connection, command.ids, name).ConfigureAwait(false);
                    return await SQLUtils.ReadEntitiesAsync(reader, command).ConfigureAwait(false);
                } else {
                    var sql = new StringBuilder();
                    sql.Append($"SELECT {ID}, {DATA} FROM {name} WHERE {ID} in\n");
                    SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.PrefixN);
                    using var reader = await connection.ExecuteReaderSync(sql.ToString()).ConfigureAwait(false);
                    return SQLUtils.ReadEntitiesSync(reader, command);
                }    
            } catch (SqlException e) {
                return new ReadEntitiesResult { Error = new TaskExecuteError(e.Message) };
            }
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "(1=1)" : filter.SQLServerFilter();
            var sql     = SQLServerUtils.QueryEntities(command, name, where);
            try {
                List<EntityValue> entities;
                if (ExecuteAsync) {
                    using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                    entities = await SQLUtils.QueryEntitiesAsync(reader).ConfigureAwait(false);
                } else {
                    using var reader = await connection.ExecuteReaderSync(sql).ConfigureAwait(false);
                    entities = SQLUtils.QueryEntitiesSync(reader);
                }
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            }
            catch (SqlException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            try {
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
            catch (SqlException e) {
                return new AggregateEntitiesResult { Error = new TaskExecuteError(e.Message) };
            }
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (command.all == true) {
                    var sql = $"DELETE from {name}";
                    var result = await Execute(connection, sql).ConfigureAwait(false);
                    if (result.Failed) { return new DeleteEntitiesResult { Error = result.error }; }
                    return new DeleteEntitiesResult();    
                } else {
                    await database.CreateTableTypes().ConfigureAwait(false);
                    await DeleteEntitiesCmdAsync(connection, command.ids, name).ConfigureAwait(false);
                    return new DeleteEntitiesResult();
                }
            }
            catch (SqlException e) {
                return new DeleteEntitiesResult { Error = new TaskExecuteError(e.Message) };
            }
        }
        
        private static TaskExecuteError DatabaseError(string message) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, message);
        }
    }
}

#endif