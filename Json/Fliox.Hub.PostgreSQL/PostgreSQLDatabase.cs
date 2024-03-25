// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Npgsql;
using static Friflo.Json.Fliox.Hub.PostgreSQL.PostgreSQLUtils;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public sealed partial class PostgreSQLDatabase : EntityDatabase, ISQLDatabase
    {
        public              TableType   TableType               { get; init; } = TableType.Relational;
        
        private  readonly   string      connectionString;
        private  readonly   ConnectionPool<SyncConnection> connectionPool;
        
        public   override   string      StorageType => "PostgreSQL - " + TableType;
        
        public PostgreSQLDatabase(string dbName, string  connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<PostgreSQLDatabase>(schema), service)
        {
            var builder             = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false };
            this.connectionString   = builder.ConnectionString;
            connectionPool          = new ConnectionPool<SyncConnection>();
        }
        
        public override void Dispose() {
            base.Dispose();
            connectionPool.ClearAll();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new PostgreSQLContainer(name.AsString(), this);
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
        
        private SyncConnectionError OpenError(PostgresException e) {
            if (e.SqlState == PostgresErrorCodes.InvalidCatalogName) {
                return SyncConnectionError.DatabaseDoesNotExist(name);
            } 
            return new SyncConnectionError(e);
        }
        
        protected  override void ReturnConnection(ISyncConnection connection) {
            connectionPool.Push(connection);
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
        }
        
        public override async Task DropDatabaseAsync() {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var db      = builder.Database;
            builder.Remove("Database");
            using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            var sql = $"drop database {db};"; // WITH (FORCE);";
            using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteReaderAsync().ConfigureAwait(false);
        }
        
        protected override async Task DropContainerAsync(ISyncConnection syncConnection, string name) {
            if (syncConnection is not SyncConnection connection) {
                throw new InvalidOperationException(syncConnection.Error.message); 
            }
            var sql = $"DROP TABLE IF EXISTS \"{name}\";";
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }
        
        public async Task CreateFunctions(ISyncConnection connection) {
            await CreateText2Ts(connection).ConfigureAwait(false);
        }
        
        private async Task CreateText2Ts(ISyncConnection synConnection) {
            if (TableType != TableType.JsonColumn) {
                return;
            }
            // [Why isn't it possible to cast to a timestamp in an index in PostgreSQL? - Database Administrators Stack Exchange]
            // https://dba.stackexchange.com/questions/250627/why-isnt-it-possible-to-cast-to-a-timestamp-in-an-index-in-postgresql
            const string sql =
                @"CREATE or REPLACE FUNCTION text2ts(text) RETURNS timestamp without time zone
    LANGUAGE sql IMMUTABLE AS $$SELECT CASE WHEN $1 ~ '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z$'
        THEN CAST($1 AS timestamp without time zone)
        END$$;";
            var connection = (SyncConnection)synConnection;
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }
        
        // ------------------------------------------ sync / async ------------------------------------------
        protected override async Task<ISyncConnection> GetConnectionAsync()
        {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            try {
                var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);                
            }
            catch(PostgresException e) {
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
            catch (PostgresException e) {
                return new TransResult(e.MessageText);
            }
        }
        
        public override async Task<Result<RawSqlResult>> ExecuteRawSQLAsync(RawSql sql, SyncContext syncContext)
        {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return Result.Error(syncConnection.Error.message);
            }
            try {
                using var reader = await connection.ExecuteReaderAsync(sql.command).ConfigureAwait(false);
                return await ReadRowsAsync(reader).ConfigureAwait(false);
            }
            catch (PostgresException e) {
                return Result.Error(e.MessageText);
            }
        }
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
