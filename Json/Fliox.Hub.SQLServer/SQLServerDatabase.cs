// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Microsoft.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;

namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public sealed class SQLServerDatabase : EntityDatabase
    {
        public              bool            Pretty      { get; init; } = false;
        
        private  readonly   string          connectionString;
        private             bool            tableTypesCreated;
        
        public   override   string          StorageType => "Microsoft SQL Server";
        
        public SQLServerDatabase(string dbName, string connectionString, DatabaseService service = null)
            : base(dbName, service)
        {
            this.connectionString = connectionString;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLServerContainer(name.AsString(), this, Pretty);
        }
        
        public override async Task<SyncConnection> GetConnection()  {
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
            try {
                await CreateDatabaseIfNotExistsAsync(connectionString).ConfigureAwait(false);
            } catch (Exception e) {
                connection?.Dispose();
                openException = e;
                return new SyncConnection(new TaskExecuteError(openException.Message));
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
            return new SyncConnection(new TaskExecuteError("timeout open newly created database"));
        }

        internal async Task CreateTableTypes() {
            if (tableTypesCreated) {
                return;
            }
            var connection = await GetConnection().ConfigureAwait(false);
            var sql = "IF TYPE_ID(N'KeyValueType') IS NULL CREATE TYPE KeyValueType AS TABLE(id varchar(128), data varchar(max));";
            using (var cmd = Command(sql, connection)) {
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            sql = "IF TYPE_ID(N'KeyType') IS NULL CREATE TYPE KeyType AS TABLE(id varchar(128));";
            using (var cmd = Command(sql, connection)) {
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            tableTypesCreated = true;
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
