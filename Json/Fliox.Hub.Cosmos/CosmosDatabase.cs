// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Hub.Host;
using Microsoft.Azure.Cosmos;

namespace Friflo.Json.Fliox.Hub.Cosmos
{
    public sealed class CosmosDatabase : EntityDatabase
    {
        public   bool                   Pretty { get; init; } = false;
        
        private  readonly   Database    cosmosDatabase;
        private  readonly   int?        throughput;
        
        public   override   string      StorageType => "CosmosDB";
        
        public CosmosDatabase(string dbName, Database cosmosDatabase, DatabaseService service = null, int? throughput = null)
            : base(dbName, service)
        {
            this.cosmosDatabase = cosmosDatabase;
            this.throughput     = throughput;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            var options = new ContainerOptions(cosmosDatabase, throughput);
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
    // this is needed to enable the record feature in .NET framework and .NET core <= 3.1 projects
    internal static class IsExternalInit { }
}

#endif
