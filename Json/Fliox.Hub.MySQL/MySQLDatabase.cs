// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.MySQL.MySQLUtils;

namespace Friflo.Json.Fliox.Hub.MySQL
{
    public class MySQLDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool            Pretty                  { get; init; } = false;
        public              TableType       TableType               { get; init; } = TableType.JsonColumn;
        private  readonly   ConnectionPool<SyncConnection> connectionPool; 
        
        private  readonly   string          connectionString;
        
        public   override   string          StorageType => "MySQL";
        internal virtual    MySQLProvider   Provider    => MySQLProvider.MY_SQL;
        
        public MySQLDatabase(string dbName, string connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<MySQLDatabase>(schema), service)
        {
            var builder             = new MySqlConnectionStringBuilder(connectionString) { Pooling = false };
            this.connectionString   = builder.ConnectionString;
            connectionPool          = new ConnectionPool<SyncConnection>();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new MySQLContainer(name.AsString(), this, Pretty);
        }
        
        public override async Task<ISyncConnection> GetConnectionAsync()  {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            try {
                var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);                
            } catch (Exception e) {
                return new SyncConnectionError(e);
            }
        }
        
        public override void ReturnConnection(ISyncConnection connection) {
            connectionPool.Push(connection);
        }
        
        public override async Task<TransResult> Transaction(SyncContext syncContext, TransCommand command) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new TransResult(syncConnection.Error.message);
            }
            var sql = command switch {
                TransCommand.Begin      => "START TRANSACTION;",
                TransCommand.Commit     => "COMMIT;",
                TransCommand.Rollback   => "ROLLBACK;",
                _                       => null
            };
            if (sql == null) return new TransResult($"invalid transaction command {command}");
            try {
                await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
                return new TransResult(command);
            }
            catch (MySqlException e) {
                return new TransResult(e.Message);
            }
        }
        
        protected override async Task CreateNewAsync() {
            await CreateDatabaseIfNotExistsAsync(connectionString).ConfigureAwait(false);
        }

        public override async Task DropDatabase() {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            var db      = builder.Database;
            builder.Remove("Database");
            var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            var sql = $"drop database {db};";
            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteReaderAsync().ConfigureAwait(false);
        }
    }
    
    public sealed class MariaDBDatabase : MySQLDatabase
    {
        public    override   string          StorageType => "MariaDB";
        internal  override   MySQLProvider   Provider    => MySQLProvider.MARIA_DB;
        
        public MariaDBDatabase(string dbName, string connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, connectionString, AssertSchema<MariaDBDatabase>(schema), service)
        { }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new MySQLContainer(name.AsString(), this, Pretty);
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
