// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Hub.Host;
using Microsoft.Azure.Cosmos;

namespace Friflo.Json.Fliox.Hub.Cosmos
{
    public sealed class CosmosDatabase : EntityDatabase
    {
        public              bool        Pretty      { get; init; } = false;
        public              int?        Throughput  { get; init; } = null;
        
        private  readonly   Database    cosmosDatabase;
        
        public   override   string      StorageType => "CosmosDB";
        
        public CosmosDatabase(string dbName, Database cosmosDatabase, DatabaseService service = null)
            : base(dbName, service)
        {
            this.cosmosDatabase = cosmosDatabase;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            var options = new ContainerOptions(cosmosDatabase, Throughput);
            return new CosmosContainer(name.AsString(), database, options, Pretty);
        }
    }
    
    internal sealed class ContainerOptions {
        internal readonly   Database    database;
        internal readonly   int?        throughput;
        
        internal  ContainerOptions (Database database, int? throughput) {
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
