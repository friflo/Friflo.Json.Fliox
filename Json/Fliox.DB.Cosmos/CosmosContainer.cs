// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Microsoft.Azure.Cosmos;

namespace Friflo.Json.Fliox.DB.Cosmos
{
    public class CosmosDatabase : EntityDatabase
    {
        private  readonly   bool        pretty;
        private  readonly   Database    cosmosDatabase;
        private  readonly   int?        throughput;

        public CosmosDatabase(Database cosmosDatabase, int? throughput = null, bool pretty = false) {
            this.cosmosDatabase = cosmosDatabase;
            this.throughput     = throughput;
            this.pretty         = pretty;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            var options = new ContainerOptions(cosmosDatabase, throughput);
            return new CosmosContainer(name, database, options, pretty);
        }
    }
    
    internal class ContainerOptions {
        internal readonly   Database    database;
        internal readonly   int?        throughput;
        
        internal  ContainerOptions (Database database, int? throughput) {
            this.database   = database;
            this.throughput = throughput;
        }
    }
    
    public class CosmosContainer : EntityContainer
    {
        private  readonly   ContainerOptions    options;
        private             Container           cosmosContainer;
        public   override   bool                Pretty      { get; }
        
        private static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding (false, true);

        internal CosmosContainer(string name, EntityDatabase database, ContainerOptions options, bool pretty) : base(name, database) {
            this.options    = options;
            Pretty          = pretty;
        }

        private async Task EnsureContainerExists() {
            if (cosmosContainer != null)
                return;
            var db          = options.database;
            var throughput  = options.throughput;
            cosmosContainer = await db.CreateContainerIfNotExistsAsync(name, "/id", throughput).ConfigureAwait(false);
        }
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            using(var memory   = new ReusedMemoryStream())
            using(var writer   = new StreamWriter(memory, Utf8Encoding, -1, true)) {
                for (int n = 0; n < entities.Count; n++) {
                    var key     = command.entityKeys[n];
                    var payload = entities[n];
                    CosmosUtils.WriteJson(writer, memory, payload.json);
                    var partitionKey = new PartitionKey(key.AsString());
                    // consider using [Introducing Bulk support in the .NET SDK | Azure Cosmos DB Blog] https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/
                    // todo handle error;
                    using (var _ = await cosmosContainer.CreateItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            return new CreateEntitiesResult();
        }

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            using(var memory   = new ReusedMemoryStream())
            using(var writer   = new StreamWriter(memory, Utf8Encoding, -1, true)) {
                for (int n = 0; n < entities.Count; n++) {
                    var key     = command.entityKeys[n];
                    var payload = entities[n];
                    CosmosUtils.WriteJson(writer, memory, payload.json);
                    var partitionKey = new PartitionKey(key.AsString());
                    // consider using [Introducing Bulk support in the .NET SDK | Azure Cosmos DB Blog] https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/
                    // todo handle error;
                    using (var _ = await cosmosContainer.UpsertItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var keys = command.ids;
            if (keys.Count > 1) {
                return await ReadManyEntities(command, messageContext).ConfigureAwait(false);
            }
            // optimization: single item read requires no parsing of Response message
            var entities        = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            var key             = keys.First();
            var id              = key.AsString();
            var partitionKey    = new PartitionKey(id);
            // todo handle error;
            using (var response = await cosmosContainer.ReadItemStreamAsync(id, partitionKey).ConfigureAwait(false)) {
                var content = response.Content;
                if (content == null) {
                    entities.TryAdd(key, new EntityValue());
                } else {
                    using (StreamReader reader = new StreamReader(content)) {
                        string payload = await reader.ReadToEndAsync().ConfigureAwait(false);
                        var entry = new EntityValue(payload);
                        entities.TryAdd(key, entry);
                    }
                }
            }
            return new ReadEntitiesResult{entities = entities};
        }
        
        private async Task<ReadEntitiesResult> ReadManyEntities(ReadEntities command, MessageContext messageContext) {
            var keys        = command.ids;
            var entities    = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            var list        = new List<(string, PartitionKey)>(keys.Count);
            foreach (var key in keys) {
                var id = key.AsString();
                list.Add((id, new PartitionKey(id)));
            }
            // todo handle error;
            using (var response     = await cosmosContainer.ReadManyItemsStreamAsync(list).ConfigureAwait(false))
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                var reader      = pooledMapper.instance.reader;
                var documents   = await CosmosUtils.ReadDocuments(reader, response.Content).ConfigureAwait(false);
                CosmosUtils.AddEntities(documents, "id", entities, messageContext);
                foreach (var key in keys) {
                    if (entities.ContainsKey(key))
                        continue;
                    entities.Add(key, new EntityValue());
                }
            }
            return new ReadEntitiesResult{entities = entities};
        }

        private readonly bool filterByClient = false; // true: used for development => query all and filter thereafter
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities    = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
            var documents   = new List<JsonValue>();
            var sql         = filterByClient ? null : command.filter.query.Cosmos;
            using (FeedIterator iterator    = cosmosContainer.GetItemQueryStreamIterator(sql))
            using (var pooledMapper         = messageContext.pools.ObjectMapper.Get()) {
                while (iterator.HasMoreResults) {
                    using(ResponseMessage response = await iterator.ReadNextAsync().ConfigureAwait(false)) {
                        var reader  = pooledMapper.instance.reader;
                        var docs    = await CosmosUtils.ReadDocuments(reader, response.Content).ConfigureAwait(false);
                        if (docs == null)
                            throw new InvalidOperationException($"no Documents in Cosmos ResponseMessage. command: {command}");
                        documents.AddRange(docs);
                    }
                }
            }
            CosmosUtils.AddEntities(documents, "id", entities, messageContext);
            if (filterByClient) {
                return FilterEntities(command, entities, messageContext);
            }
            return new QueryEntitiesResult{entities = entities};
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
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
        
        public override Task<AutoIncrementResult>   AutoIncrement  (AutoIncrement  command, MessageContext messageContext) {
            throw new NotImplementedException("");
        }
    }
}

#endif