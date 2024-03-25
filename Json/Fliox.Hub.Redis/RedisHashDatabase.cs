// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using StackExchange.Redis;

namespace Friflo.Json.Fliox.Hub.Redis
{
    public sealed class RedisHashDatabase : EntityDatabase
    {
        public              bool        Pretty      { get; init; } = false;
        /// <summary>The ID to get a database for. See <see cref="ConnectionMultiplexer.GetDatabase"/></summary>
        public              int         DbIndex     { get; init; } = -1;
        
        private  readonly   string      connectionString;
        private  readonly   ConnectionPool<SyncConnection> connectionPool; 
        
        public   override   string      StorageType => "Redis";
        
        public RedisHashDatabase(string dbName, string connectionString, DatabaseSchema schema = null, DatabaseService service = null)
            : base(dbName, schema, service)
        {
            this.connectionString   = connectionString;
            connectionPool          = new ConnectionPool<SyncConnection>();
        }
        
        public override void Dispose() {
            base.Dispose();
            connectionPool.ClearAll();
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new RedisHashContainer(name.AsString(), this, Pretty);
        }
        
        protected override async Task<ISyncConnection> GetConnectionAsync()  {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            try {
                var instance = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
                return new SyncConnection(instance);
            }
            catch (RedisException e) {
                return new SyncConnectionError(e.Message);
            }
        }
        
        protected  override void ReturnConnection(ISyncConnection connection) {
            connectionPool.Push(connection);
        }
    }
    
    internal sealed class SyncConnection : ISyncConnection
    {
        internal readonly    ConnectionMultiplexer         instance;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Dispose();
        public  bool                IsOpen      => instance.IsConnected;
        public  void                ClearPool() { }
        
        public SyncConnection (ConnectionMultiplexer instance) {
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
