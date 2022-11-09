// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
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
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            var options = new ContainerOptions(cosmosDatabase, throughput);
            return new CosmosContainer(name, database, options, pretty);
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
    
    public sealed class CosmosContainer : EntityContainer
    {
        private  readonly   ContainerOptions    options;
        private             Container           cosmosContainer;
        public   override   bool                Pretty      { get; }
        
        internal CosmosContainer(string name, EntityDatabase database, ContainerOptions options, bool pretty)
            : base(name, database)
        {
            this.options    = options;
            Pretty          = pretty;
        }

        private async Task EnsureContainerExists() {
            if (cosmosContainer != null)
                return;
            var db          = options.database;
            var throughput  = options.throughput;
            cosmosContainer = await db.CreateContainerIfNotExistsAsync(instanceName, "/id", throughput).ConfigureAwait(false);
        }
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities = command.entities;
            using(var memory   = new ReusedMemoryStream()) {
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    var key     = entity.key;
                    CosmosUtils.WriteJson(memory, entity.value);
                    var partitionKey = new PartitionKey(key.AsString());
                    // consider using [Introducing Bulk support in the .NET SDK | Azure Cosmos DB Blog] https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/
                    // todo handle error;
                    using (var _ = await cosmosContainer.CreateItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            return new CreateEntitiesResult();
        }

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities = command.entities;
            using (var memory           = new ReusedMemoryStream())
            using (var pooled  = syncContext.EntityProcessor.Get()) {
                var processor = pooled.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    var key     = entity.key;
                    var json = processor.ReplaceKey(entity.value, command.keyName, false, "id", out _, out string error);
                    CosmosUtils.WriteJson(memory, json);
                    var partitionKey = new PartitionKey(key.AsString());
                    // consider using [Introducing Bulk support in the .NET SDK | Azure Cosmos DB Blog] https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/
                    // todo handle error;
                    using (var _ = await cosmosContainer.UpsertItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var keys = command.ids;
            if (keys.Count > 1) {
                return await ReadManyEntities(command, syncContext).ConfigureAwait(false);
            }
            // optimization: single item read requires no parsing of Response message
            var entities        = new List<EntityValue>(keys.Count);
            var key             = keys[0];
            var id              = key.AsString();
            var partitionKey    = new PartitionKey(id);
            // todo handle error;
            using (var pooled   = syncContext.EntityProcessor.Get())
            using (var response = await cosmosContainer.ReadItemStreamAsync(id, partitionKey).ConfigureAwait(false)) {
                var processor   = pooled.instance;
                var content     = response.Content;
                if (content == null) {
                    entities.Add(new EntityValue(key));
                } else {
                    var     payload     = await EntityUtils.ReadToEnd(content).ConfigureAwait(false);
                    bool    asIntKey    = command.isIntKey == true; 
                    var     json        = processor.ReplaceKey(payload, "id", asIntKey, command.keyName, out _, out _);
                    var     entry       = new EntityValue(key, json);
                    entities.Add(entry);
                }
            }
            return new ReadEntitiesResult{entities = entities.ToArray() };
        }
        
        private async Task<ReadEntitiesResult> ReadManyEntities(ReadEntities command, SyncContext syncContext) {
            var keys        = command.ids;
            var entities    = new List<EntityValue>(keys.Count);
            var list        = new List<(string, PartitionKey)>(keys.Count);
            foreach (var key in keys) {
                var id = key.AsString();
                list.Add((id, new PartitionKey(id)));
            }
            // todo handle error;
            using (var response = await cosmosContainer.ReadManyItemsStreamAsync(list).ConfigureAwait(false))
            using (var pooled   = syncContext.ObjectMapper.Get()) {
                var reader      = pooled.instance.reader;
                var documents   = await CosmosUtils.ReadDocuments(reader, response.Content).ConfigureAwait(false);
                EntityUtils.AddEntities(documents, "id", command.isIntKey, command.keyName, entities, syncContext);
                /* foreach (var key in keys) {
                    if (entities.ContainsKey(key))
                        continue;
                    entities.Add(new EntityValue(key));
                } */
            }
            return new ReadEntitiesResult{entities = entities.ToArray() };
        }

        private readonly bool filterByClient = false; // true: used for development => query all and filter thereafter
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities    = new List<EntityValue>();
            var documents   = new List<JsonValue>();
            var sql         = filterByClient ? null : "SELECT * FROM c WHERE " + command.GetFilter().query.Cosmos;
            using (FeedIterator iterator    = cosmosContainer.GetItemQueryStreamIterator(sql))
            using (var pooled               = syncContext.ObjectMapper.Get()) {
                while (iterator.HasMoreResults) {
                    using(ResponseMessage response = await iterator.ReadNextAsync().ConfigureAwait(false)) {
                        var reader  = pooled.instance.reader;
                        var docs    = await CosmosUtils.ReadDocuments(reader, response.Content).ConfigureAwait(false);
                        if (docs == null)
                            throw new InvalidOperationException($"no Documents in Cosmos ResponseMessage. command: {command}");
                        documents.AddRange(docs);
                    }
                }
            }
            EntityUtils.AddEntities(documents, "id", command.isIntKey, command.keyName, entities, syncContext);
            if (filterByClient) {
                throw new NotImplementedException();
                // return FilterEntities(command, entities, syncContext);
            }
            return new QueryEntitiesResult{entities = entities.ToArray() };
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            var result = new AggregateEntitiesResult { Error = new CommandError($"aggregate {command.type} not implement") };
            return Task.FromResult(result);
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var keys = command.ids;
            foreach (var key in keys) {
                var id              = key.AsString();
                var partitionKey    = new PartitionKey(id);
                // todo handle error;
                using (var _ = await cosmosContainer.DeleteItemStreamAsync(id, partitionKey).ConfigureAwait(false)) {
                }
            }
            return new DeleteEntitiesResult();
        }
    }
}

#endif