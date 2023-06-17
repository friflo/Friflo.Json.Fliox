// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using System.Data.SqlClient;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;

namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public sealed class SQLServerDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool            Pretty                  { get; init; } = false;
        public              bool            AutoCreateDatabase      { get; init; } = true;
        public              bool            AutoCreateTables        { get; init; } = true;
        public              bool            AutoAddVirtualColumns   { get; init; } = true;
        
        private  readonly   string          connectionString;
        private             bool            tableTypesCreated;
        
        private  readonly   ConnectionPool<SyncConnection> connectionPool;

        public   override   string          StorageType => "Microsoft SQL Server";
        
        public SQLServerDatabase(string dbName, string connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<SQLServerDatabase>(schema), service)
        {
            var builder             = new SqlConnectionStringBuilder(connectionString) { Pooling = false };
            this.connectionString   = builder.ConnectionString;
            connectionPool          = new ConnectionPool<SyncConnection>();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLServerContainer(name.AsString(), this, Pretty);
        }
        
        public override async Task<ISyncConnection> GetConnectionAsync()  {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            Exception openException;
            SqlConnection connection= null;
            try {
                connection = new SqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);   
            } catch (SqlException e) {
                connection?.Dispose();
                openException = e;
            }
            if (!AutoCreateDatabase) {
                return new SyncConnectionError(openException);
            }
            try {
                await CreateDatabaseIfNotExistsAsync(connectionString).ConfigureAwait(false);
            } catch (Exception e) {
                connection?.Dispose();
                return new SyncConnectionError(e);
            }
            var end = DateTime.Now + new TimeSpan(0, 0, 0, 10, 0);
            while (DateTime.Now < end) {
                try {
                    connection = new SqlConnection(connectionString);
                    await connection.OpenAsync().ConfigureAwait(false);
                    return new SyncConnection(connection);
                } catch (SqlException) {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            return new SyncConnectionError(new TaskExecuteError("timeout open newly created database"));
        }
        
        public override void ReturnConnection(ISyncConnection connection) {
            connectionPool.Push(connection);
        }

        internal async Task CreateTableTypes() {
            if (tableTypesCreated) {
                return;
            }
            var connection = (SyncConnection)await GetConnectionAsync().ConfigureAwait(false);
            var sql = $"IF TYPE_ID(N'KeyValueType') IS NULL CREATE TYPE KeyValueType AS TABLE({SQLServerContainer.ColumnId}, {SQLServerContainer.ColumnData});";
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);

            sql = $"IF TYPE_ID(N'KeyType') IS NULL CREATE TYPE KeyType AS TABLE({SQLServerContainer.ColumnId});";
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
            tableTypesCreated = true;
        }
        
        public override async Task<Result<TransactionResult>> Transaction(SyncContext syncContext, TransactionCommand command) {
            var syncConnection = await syncContext.GetConnectionAsync();
            if (syncConnection is not SyncConnection connection) {
                return new TransactionResult();
            }
            var sql = command switch {
                TransactionCommand.Begin    => "BEGIN TRANSACTION;",
                TransactionCommand.Commit   => "COMMIT;",
                TransactionCommand.Rollback => "ROLLBACK;",
                _                           => null
            };
            if (sql == null) return Result.Error($"invalid transaction command {command}");
            try {
                await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                return new TransactionResult();
            }
            catch (SqlException e) {
                return Result.Error(e.Message);
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
