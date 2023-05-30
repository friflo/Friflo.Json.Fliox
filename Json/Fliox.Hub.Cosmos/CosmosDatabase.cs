// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace Friflo.Json.Fliox.Hub.Cosmos
{
    public sealed class CosmosDatabase : EntityDatabase
    {
        public              bool            Pretty      { get; init; } = false;
        public              int?            Throughput  { get; init; } = null;
        
        private  readonly   CosmosClient    client;
        private  readonly   string          cosmosDbName;
        private             Database        cosmosDb;
        
        public   override   string      StorageType => "CosmosDB";
        
        public CosmosDatabase(string dbName, CosmosClient client, string cosmosDbName = null, DatabaseService service = null)
            : base(dbName, service)
        {
            this.client         = client;
            this.cosmosDbName   = cosmosDbName ?? dbName;
        }
        
        public CosmosDatabase(string dbName, string connectionString, DatabaseService service = null)
            : base(dbName, service)
        {
            var builder     = new CosmosClientBuilder(connectionString);
            client         = builder.Build();
            cosmosDbName   = dbName;
        }
        
        internal async Task<Database> GetCosmosDb() {
            if (cosmosDb != null) {
                return cosmosDb;
            }
            return cosmosDb = await client.CreateDatabaseIfNotExistsAsync(cosmosDbName).ConfigureAwait(false);
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            var options = new ContainerOptions(this, Throughput);
            return new CosmosContainer(name.AsString(), database, options, Pretty);
        }
    }
    
    internal sealed class ContainerOptions {
        internal readonly   CosmosDatabase  database;
        internal readonly   int?            throughput;
        
        internal  ContainerOptions (CosmosDatabase database, int? throughput) {
            this.database   = database;
            this.throughput = throughput;
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
