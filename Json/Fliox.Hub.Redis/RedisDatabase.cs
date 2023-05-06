// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using StackExchange.Redis;

namespace Friflo.Json.Fliox.Hub.Redis
{
    public class RedisDatabase : EntityDatabase
    {
        public              bool                    Pretty      { get; init; } = false;
        
        internal readonly   string                  connectionString;
        
        
        public   override   string          StorageType => "Redis";
        
        public RedisDatabase(string dbName, string connectionString, DatabaseService service = null)
            : base(dbName, service)
        {
            this.connectionString = connectionString;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new RedisContainer(name.AsString(), this, Pretty);
        }
        
        public override async Task<SyncConnection> GetConnection()  {
            var instance = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
            return new SyncConnection(instance);
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
