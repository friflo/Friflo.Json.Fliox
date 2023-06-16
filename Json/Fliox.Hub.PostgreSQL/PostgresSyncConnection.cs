// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using System.Data;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Npgsql;

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    internal sealed class SyncConnection : ISyncConnection
    {
        private readonly    NpgsqlConnection         instance;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Close();
        public  bool                IsOpen      => instance.State == ConnectionState.Open;
        
        public SyncConnection (NpgsqlConnection instance) {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
        
        internal async Task ExecuteNonQueryAsync (string sql) {
            using var cmd = new NpgsqlCommand(sql, instance);
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return;
                }
                catch (NpgsqlException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        internal async Task<NpgsqlDataReader> ExecuteReaderAsync(string sql) {
            using var command = new NpgsqlCommand(sql, instance);
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    return await command.ExecuteReaderAsync().ConfigureAwait(false);
                }
                catch (NpgsqlException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
    }
}

#endif