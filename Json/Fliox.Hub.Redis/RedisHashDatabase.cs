// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using StackExchange.Redis;

namespace Friflo.Json.Fliox.Hub.Redis
{
    public class RedisHashDatabase : EntityDatabase
    {
        public              bool        Pretty      { get; init; } = false;
        
        private  readonly   string      connectionString;
        internal readonly   int         databaseNumber;
        
        
        public   override   string      StorageType => "Redis";
        
        public RedisHashDatabase(string dbName, string connectionString, int databaseNumber = 0, DatabaseService service = null)
            : base(dbName, service)
        {
            this.connectionString   = connectionString;
            this.databaseNumber     = databaseNumber;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new RedisHashContainer(name.AsString(), this, Pretty);
        }
        
        public override async Task<SyncConnection> GetConnectionAsync()  {
            try {
                var instance = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
                return new SyncConnection(instance);
            }
            catch (RedisException e) {
                var error = new TaskExecuteError(TaskErrorType.DatabaseError, e.Message);
                return new SyncConnection(error);
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
