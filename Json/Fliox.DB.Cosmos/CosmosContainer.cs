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
            using(var memory   = new ReusedMemoryStream())
            using(var writer   = new StreamWriter(memory, Utf8Encoding, -1, true)) {
                foreach (var entityPair in entities) {
                    var id      = entityPair.Key.AsString();
                    var payload = entityPair.Value.Json;
                    memory.SetLength(0);
                    writer.Write(payload);
                    writer.Flush();
                    memory.Seek(0, SeekOrigin.Begin);
                    var partitionKey = new PartitionKey(id);
                    // todo handle error;
                    using (var _ = await cosmosContainer.CreateItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            var result = new CreateEntitiesResult();
            return result;
        }

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities = command.entities;
            using(var memory   = new ReusedMemoryStream())
            using(var writer   = new StreamWriter(memory, Utf8Encoding, -1, true)) {
                foreach (var entityPair in entities) {
                    var id      = entityPair.Key.AsString();
                    var payload = entityPair.Value.Json;
                    memory.SetLength(0);
                    writer.Write(payload);
                    writer.Flush();
                    memory.Seek(0, SeekOrigin.Begin);
                    var partitionKey = new PartitionKey(id);
                    // todo handle error;
                    using (var _ = await cosmosContainer.UpsertItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            var result = new UpsertEntitiesResult();
            return result;
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var keys = command.ids;
            if (keys.Count > 1) {
                return await ReadManyEntities(command, messageContext);
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
            var result = new ReadEntitiesResult{entities = entities};
            return result;
        }
        
        private async Task<ReadEntitiesResult> ReadManyEntities(ReadEntities command, MessageContext messageContext) {
            var keys        = command.ids;
            var entities    = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            var list        = new List<(string, PartitionKey)>(keys.Count);
            foreach (var key in keys) {
                var id = key.AsString();
                list.Add((id, new PartitionKey(id)));
            }
            List<JsonValue> documents;
            // todo handle error;
            using (var response     = await cosmosContainer.ReadManyItemsStreamAsync(list).ConfigureAwait(false))
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                documents = await ReadDocuments(pooledMapper.instance.reader, response.Content);
            }
            AddEntities(documents, entities, messageContext);
            foreach (var key in keys) {
                if (entities.ContainsKey(key))
                    continue;
                entities.Add(key, new EntityValue());
            }
            var result = new ReadEntitiesResult{entities = entities};
            return result;
        }
        
        private static async Task<List<JsonValue>> ReadDocuments(ObjectReader reader, Stream content) {
            using (StreamReader streamReader = new StreamReader(content)) {
                string documentsJson    = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                var responseFeed        = reader.Read<ResponseFeed>(documentsJson);
                return responseFeed.Documents;
            }
        }
        
        private static void AddEntities(List<JsonValue> documents, Dictionary<JsonKey, EntityValue> entities, MessageContext messageContext) {
            using (var pooledValidator = messageContext.pools.EntityValidator.Get()) {
                var validator = pooledValidator.instance;
                foreach (var document in documents) {
                    var payload = document.json;
                    if (!validator.GetEntityKey(payload, "id", out string keyValue, out _)) {
                        continue;
                    }
                    var key     = new JsonKey(keyValue);
                    var value   = new EntityValue(document.json);
                    entities.Add(key, value);
                }
            }
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities    = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
            var documents   = new List<JsonValue>();
            using (FeedIterator iterator    = cosmosContainer.GetItemQueryStreamIterator())
            using (var pooledMapper         = messageContext.pools.ObjectMapper.Get()) {
                while (iterator.HasMoreResults) {
                    using(ResponseMessage response = await iterator.ReadNextAsync().ConfigureAwait(false)) {
                        var docs = await ReadDocuments(pooledMapper.instance.reader, response.Content);
                        if (docs == null)
                            throw new InvalidOperationException($"no Documents in Cosmos ResponseMessage. command: {command}");
                        documents.AddRange(docs);
                    }
                }
            }
            AddEntities(documents, entities, messageContext);
            var result = FilterEntities(command, entities, messageContext);
            return result;
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
            var result = new DeleteEntitiesResult();
            return result;
        }
    }
}

#endif