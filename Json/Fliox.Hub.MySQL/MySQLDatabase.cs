// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.MySQL.MySQLUtils;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.MySQL
{
    public class MySQLDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool            Pretty                  { get; init; } = false;
        public              TableType       TableType               { get; init; } = TableType.Relational;
        private  readonly   ConnectionPool<SyncConnection> connectionPool; 
        
        private  readonly   string          connectionString;
        
        public   override   string          StorageType => "MySQL - " + TableType;
        internal virtual    MySQLProvider   Provider    => MySQLProvider.MY_SQL;
        
        public MySQLDatabase(string dbName, string connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<MySQLDatabase>(schema), service)
        {
            var builder             = new MySqlConnectionStringBuilder(connectionString) { Pooling = false };
            this.connectionString   = builder.ConnectionString;
            connectionPool          = new ConnectionPool<SyncConnection>();
        }
        
        public override void Dispose() {
            base.Dispose();
            connectionPool.ClearAll();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new MySQLContainer(name.AsString(), this, Pretty);
        }
        
        protected override async Task<ISyncConnection> GetConnectionAsync()  {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            try {
                var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);                
            }
            catch(MySqlException e) {
                if (e.ErrorCode == MySqlErrorCode.UnknownDatabase) {
                    return SyncConnectionError.DatabaseDoesNotExist(name);
                } 
                return new SyncConnectionError(e);
            }
            catch (Exception e) {
                return new SyncConnectionError(e);
            }
        }
        
        protected  override void ReturnConnection(ISyncConnection connection) {
            connectionPool.Push(connection);
        }
        
        protected override async Task<TransResult> Transaction(SyncContext syncContext, TransCommand command) {
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
                var msg = GetErrMsg(e);
                return new TransResult(msg);
            }
        }
        
        protected override async Task CreateDatabaseAsync() {
            await CreateDatabaseIfNotExistsAsync(connectionString).ConfigureAwait(false);
        }

        public override async Task DropDatabaseAsync() {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            var db      = builder.Database;
            builder.Remove("Database");
            using var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            var sql = $"drop database {db};";
            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteReaderAsync().ConfigureAwait(false);
        }
        
        protected override async Task DropContainerAsync(ISyncConnection syncConnection, string name) {
            if (syncConnection is not SyncConnection connection) {
                throw new InvalidOperationException(syncConnection.Error.message); 
            }
            var sql = $"DROP TABLE IF EXISTS `{name}`;";
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }
        
        public override async Task<Result<RawSqlResult>> ExecuteRawSQL(string sql, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new RawSqlResult();
            }
            try {
                using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                var types   = SQLTable.GetFieldTypes(reader);
                var values  = new List<JsonKey>();
                var result  = new RawSqlResult { types = types};
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    SQLTable.AddRow(reader, types, values);
                    result.rowCount++;
                }
                result.values = values.ToArray();
                return result;
            }
            catch (MySqlException e) {
                var msg = GetErrMsg(e);
                return Result.Error(msg);
            }
        }

        public Task CreateFunctions(ISyncConnection connection) => Task.CompletedTask;
    }
    
    public sealed class MariaDBDatabase : MySQLDatabase
    {
        public    override   string          StorageType => "MariaDB - " + TableType;
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
