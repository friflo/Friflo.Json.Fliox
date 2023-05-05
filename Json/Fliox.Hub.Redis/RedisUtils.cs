// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using StackExchange.Redis;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.Redis
{
    public static class RedisUtils
    {
        internal static DbCommand Command (string sql, SyncConnection connection) {
            throw new NotImplementedException();
        }
        
        internal static Task<SQLResult> Execute(SyncConnection connection, string sql) {
            throw new NotImplementedException();
        }
        
        /*        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new RedisConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"CREATE DATABASE IF NOT EXISTS {database}";
            using var cmd = new RedisCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        
        private static string GetDbmsConnectionString(string connectionString, out string database) {
            var builder  = new RedisConnectionStringBuilder(connectionString);
            database = builder.Database;
            builder.Remove("Database");
            return builder.ToString();
        }
        */
    }
}

#endif