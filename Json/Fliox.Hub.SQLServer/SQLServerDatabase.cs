// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using System.Data.SqlClient;
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
        
        private  readonly   ConcurrentStack<SyncConnection> connectionPool;

        public   override   string          StorageType => "Microsoft SQL Server";
        
        public SQLServerDatabase(string dbName, string connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<SQLServerDatabase>(schema), service)
        {
            this.connectionString   = connectionString;
            connectionPool          = new ConcurrentStack<SyncConnection>();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLServerContainer(name.AsString(), this, Pretty);
        }
        
        public override async Task<ISyncConnection> GetConnectionAsync()  {
            if (connectionPool.TryPop(out var syncConnection) && syncConnection.IsOpen) {
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
            connectionPool.Push((SyncConnection)connection);
        }

        internal async Task CreateTableTypes() {
            if (tableTypesCreated) {
                return;
            }
            var connection = (SyncConnection)await GetConnectionAsync().ConfigureAwait(false);
            var sql = $"IF TYPE_ID(N'KeyValueType') IS NULL CREATE TYPE KeyValueType AS TABLE({SQLServerContainer.ColumnId}, {SQLServerContainer.ColumnData});";
            using (var cmd = Command(sql, connection)) {
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            sql = $"IF TYPE_ID(N'KeyType') IS NULL CREATE TYPE KeyType AS TABLE({SQLServerContainer.ColumnId});";
            using (var cmd = Command(sql, connection)) {
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            tableTypesCreated = true;
        }
    }
    
    internal sealed class SyncConnection : ISyncConnection
    {
        internal readonly    SqlConnection         instance;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Dispose();
        public  bool                IsOpen      => instance.State == ConnectionState.Open;
        
        public SyncConnection (SqlConnection instance) {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
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
