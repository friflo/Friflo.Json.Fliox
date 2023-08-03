// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public abstract class SyncDbConnection : ISyncConnection
    {
        private readonly   DbConnection   instance;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Dispose();
        public  bool                IsOpen      => instance.State == ConnectionState.Open;
        public  abstract void       ClearPool();
        
        protected SyncDbConnection (DbConnection instance) {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
        
        public async Task ExecuteNonQueryAsync (string sql, DbParameter parameter = null) {
            using var cmd = instance.CreateCommand();
            cmd.CommandText = sql;
            if (parameter != null) {
                cmd.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return;
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Using asynchronous execution for SQL Server is significant slower.<br/>
        /// <see cref="DbCommand.ExecuteReaderAsync()"/> ~7x slower than <see cref="DbCommand.ExecuteReader()"/>.
        /// </summary>
        public async Task<DbDataReader> ExecuteReaderAsync(string sql, DbParameter parameter = null) {
            using var cmd = instance.CreateCommand();
            cmd.CommandText = sql;
            if (parameter != null) {
                cmd.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    return await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        public DbDataReader ExecuteReaderSync(string sql) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    // ReSharper disable once MethodHasAsyncOverload
                    return command.ExecuteReader();
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        instance.Open();
                        continue;
                    }
                    throw;
                }
            }
        }
        
        public DbDataReader ExecuteReaderSync(DbCommand command) {
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    // TODO check performance hit caused by many SqlBuffer instances
                    // [Reading large data (binary, text) asynchronously is extremely slow · Issue #593 · dotnet/SqlClient]
                    // https://github.com/dotnet/SqlClient/issues/593#issuecomment-1645441459
                    return command.ExecuteReader(); // CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        instance.Open();
                        continue;
                    }
                    throw;
                }
            }
        }
    }
}

#endif