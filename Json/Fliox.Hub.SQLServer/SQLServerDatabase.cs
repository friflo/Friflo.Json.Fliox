// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using System.Data.SqlClient;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public sealed partial class SQLServerDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool            Pretty                  { get; init; } = false;
        public              TableType       TableType               { get; init; } = TableType.Relational;
        
        private  readonly   string          connectionString;
        
        private  readonly   ConnectionPool<SyncConnection> connectionPool;

        public   override   string          StorageType => "Microsoft SQL Server - " + TableType;
        
        public SQLServerDatabase(string dbName, string connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<SQLServerDatabase>(schema), service)
        {
            var builder             = new SqlConnectionStringBuilder(connectionString) { Pooling = false };
            this.connectionString   = builder.ConnectionString;
            connectionPool          = new ConnectionPool<SyncConnection>();
        }
        
        public override void Dispose() {
            base.Dispose();
            connectionPool.ClearAll();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLServerContainer(name.AsString(), this, Pretty);
        }
        
        public override bool IsSyncTask(SyncRequestTask task, in PreExecute execute) {
            if (!execute.executeSync) {
                return false;
            }
            switch (task.TaskType) {
                case TaskType.read:
                case TaskType.query:
                case TaskType.create:
                case TaskType.upsert:
                case TaskType.delete:
                case TaskType.aggregate:
                case TaskType.closeCursors:
                    return true;
            }
            return false;
        }
        
        private SyncConnectionError OpenError(SqlException e) {
            if (e.Number == 4060) {
                return SyncConnectionError.DatabaseDoesNotExist(name);
            }
            return new SyncConnectionError(e);
        }
        
        protected  override void ReturnConnection(ISyncConnection connection) {
            connectionPool.Push(connection);
        }

        private static async Task CreateTableTypes(ISyncConnection synConnection) {
            var connection = (SyncConnection)synConnection;
            var sql = $"IF TYPE_ID(N'KeyValueType') IS NULL CREATE TYPE KeyValueType AS TABLE({SQLServerContainer.ColumnId}, {SQLServerContainer.ColumnData});";
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);

            sql = $"IF TYPE_ID(N'KeyType') IS NULL CREATE TYPE KeyType AS TABLE({SQLServerContainer.ColumnId});";
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }
        
        private static string GetTransactionCommand(TransCommand command) {
            return command switch {
                TransCommand.Begin      => "BEGIN TRANSACTION;",
                TransCommand.Commit     => "COMMIT;",
                TransCommand.Rollback   => "ROLLBACK;",
                _                       => null
            };
        }
        
        protected override async Task CreateDatabaseAsync() {
            await CreateDatabaseIfNotExistsAsync(connectionString).ConfigureAwait(false);

            var end = DateTime.Now + new TimeSpan(0, 0, 0, 10, 0);
            while (DateTime.Now < end) {
                try {
                    var connection = await GetConnectionAsync().ConfigureAwait(false);
                    await CreateTableTypes(connection).ConfigureAwait(false);
                    return;
                } catch (SqlException) {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            throw new PrepareDatabaseException("timeout open newly created database");
        }
        
        public override async Task DropDatabaseAsync() {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var db      = builder.InitialCatalog;
            builder.Remove("Database");
            using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            var sql = $"drop database {db};";
            using var command = new SqlCommand(sql, connection);
            await command.ExecuteReaderAsync().ConfigureAwait(false);
        }
        
        protected override async Task DropContainerAsync(ISyncConnection syncConnection, string name) {
            if (syncConnection is not SyncConnection connection) {
                throw new InvalidOperationException(syncConnection.Error.message); 
            }
            var sql = $"DROP TABLE IF EXISTS [{name}];";
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }
        
        // ------------------------------------------ sync / async ------------------------------------------
        protected override async Task<ISyncConnection> GetConnectionAsync()
        {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            try {
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);
            }
            catch (SqlException e) {
                return OpenError(e);
            }
            catch (Exception e) {
                return new SyncConnectionError(e);
            }
        }
        
        protected override async Task<TransResult> TransactionAsync(SyncContext syncContext, TransCommand command)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new TransResult(syncConnection.Error.message);
            }
            var sql = GetTransactionCommand(command);
            if (sql == null) return new TransResult($"invalid transaction command {command}");
            try {
                await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                return new TransResult(command);
            }
            catch (SqlException e) {
                return new TransResult(GetErrMsg(e));
            }
        }
        
        public override async Task<Result<RawSqlResult>> ExecuteRawSQLAsync(RawSql sql, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return Result.Error(syncConnection.Error.message);
            }
            try {
                using var reader    = await connection.ExecuteReaderAsync(sql.command).ConfigureAwait(false);
                return await SQLTable.ReadRowsAsync(reader).ConfigureAwait(false);
            }
            catch (SqlException e) {
                var msg = GetErrMsg(e);
                return Result.Error(msg);
            }
        }
        
        public Task CreateFunctions(ISyncConnection connection) => Task.CompletedTask;
    }
}

namespace System.Runtime.CompilerServices
{
    // This is needed to enable following features in .NET framework and .NET core <= 3.1 projects:
    // - init only setter properties. See [Init only setters - C# 9.0 draft specifications | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    // - record types
    internal static class IsExternalInit { }
}

#endif
