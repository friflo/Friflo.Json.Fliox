// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Hub.Host;
using Microsoft.Azure.Cosmos;

namespace Friflo.Json.Fliox.Hub.Cosmos
{
    public sealed class CosmosDatabase : EntityDatabase
    {
        private  readonly   bool        pretty;
        private  readonly   Database    cosmosDatabase;
        private  readonly   int?        throughput;
        
        public   override   string      StorageType => "CosmosDB";
        
        public CosmosDatabase(string dbName, Database cosmosDatabase, DatabaseService service = null, DbOpt opt = null, int? throughput = null, bool pretty = false)
            : base(dbName, service, opt)
        {
            this.cosmosDatabase = cosmosDatabase;
            this.throughput     = throughput;
            this.pretty         = pretty;
        }
        
        public override EntityContainer CreateContainer(in JsonKey name, EntityDatabase database) {
            var options = new ContainerOptions(cosmosDatabase, throughput);
            return new CosmosContainer(name.AsString(), database, options, pretty);
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

#endif
