// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Npgsql;
using static Friflo.Json.Fliox.Hub.PostgreSQL.PostgreSQLUtils;

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public sealed class PostgreSQLDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool        AutoCreateDatabase      { get; init; } = true;
        public              bool        AutoCreateTables        { get; init; } = true;
        public              bool        AutoAddVirtualColumns   { get; init; } = true;
        
        private  readonly   string      connectionString;
        private  readonly   ConcurrentStack<SyncConnection> connectionPool;
        
        public   override   string      StorageType => "PostgreSQL";
        
        public PostgreSQLDatabase(string dbName, string  connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<PostgreSQLDatabase>(schema), service)
        {
            this.connectionString   = connectionString;
            connectionPool          = new ConcurrentStack<SyncConnection>();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new PostgreSQLContainer(name.AsString(), this);
        }
        
        public override async Task<ISyncConnection> GetConnectionAsync()  {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            Exception openException;
            try {
                var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);                
            } catch (Exception e) {
                openException = e;
            }
            if (!AutoCreateDatabase) {
                return new SyncConnectionError(openException);
            }
            try {
                await CreateDatabaseIfNotExistsAsync(connectionString).ConfigureAwait(false);
            } catch (Exception e) {
                return new SyncConnectionError(e);
            }
            try {
                var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);
            } catch (Exception e) {
                return new SyncConnectionError(e);
            }
        }
        
        public override void CloseConnection(ISyncConnection connection) {
            connectionPool.Push((SyncConnection)connection);
        }
    }
    
    internal sealed class SyncConnection : ISyncConnection
    {
        internal readonly    NpgsqlConnection         instance;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Dispose();
        
        public SyncConnection (NpgsqlConnection instance) {
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
