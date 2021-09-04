// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Database;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Microsoft.Azure.Cosmos;

namespace Friflo.Json.Fliox.DB.Cosmos
{
    public class CosmosDatabase : EntityDatabase
    {
        private  readonly   bool                            pretty;
        private  readonly   Microsoft.Azure.Cosmos.Database cosmosDatabase;

        public CosmosDatabase(Microsoft.Azure.Cosmos.Database cosmosDatabase, bool pretty = false) {
            this.cosmosDatabase = cosmosDatabase;
            this.pretty         = pretty;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            var container = cosmosDatabase.CreateContainerIfNotExistsAsync(name, "/id", 400).Result; // todo make CreateContainer async
            return new CosmosContainer(name, database, container, pretty);
        }
    }
    
    public class CosmosContainer : EntityContainer
    {
        private  readonly   Container   cosmosContainer;
        public   override   bool        Pretty      { get; }
        
        private static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding (false, true);

        public CosmosContainer(string name, EntityDatabase database, Container container, bool pretty) : base(name, database) {
            cosmosContainer = container;
            Pretty          = pretty;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            using(var memory   = new MemoryStream())
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
                    await cosmosContainer.CreateItemStreamAsync(memory, partitionKey);
                }
            }
            var result = new CreateEntitiesResult();
            return result;
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            using(var memory   = new MemoryStream())
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
                    await cosmosContainer.CreateItemStreamAsync(memory, partitionKey);
                }
            }
            var result = new UpdateEntitiesResult();
            return result;
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            var keys = command.ids;
            var entities = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            foreach (var key in keys) {
                var id              = key.AsString();
                var partitionKey    = new PartitionKey(id);
                // todo handle error;
                ResponseMessage response = await cosmosContainer.ReadItemStreamAsync(id, partitionKey);
                var content = response.Content;
                if (content == null) {
                    entities.TryAdd(key, new EntityValue());
                } else {
                    using (StreamReader reader = new StreamReader(content)) {
                        string payload = await reader.ReadToEndAsync();
                        var entry = new EntityValue(payload);
                        entities.TryAdd(key, entry);
                    }
                }
            }
            var result = new ReadEntitiesResult{entities = entities};
            return result;
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            var                 entities    = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
            FeedIterator        iterator    = cosmosContainer.GetItemQueryStreamIterator();
            DocumentContainer   documents   = null;
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                var reader = pooledMapper.instance.reader;
                while (iterator.HasMoreResults) {
                    using(ResponseMessage response = await iterator.ReadNextAsync()) {
                        Stream content = response.Content;
                        using (var streamReader = new StreamReader(content)) {
                            string documentsJson = await streamReader.ReadToEndAsync();
                            documents = reader.Read<DocumentContainer>(documentsJson);
                        }
                    }
                }
            }
            if (documents == null)
                throw new InvalidOperationException("no Documents in Cosmos ResponseMessage");

            using (var pooledValidator = messageContext.pools.EntityValidator.Get()) {
                var validator = pooledValidator.instance;
                foreach (var document in documents.Documents) {
                    var payload = document.json;
                    if (!validator.GetEntityKey(payload, "id", out string keyValue, out _)) {
                        continue;
                    }
                    var key     = new JsonKey(keyValue);
                    var value   = new EntityValue(document.json);
                    entities.Add(key, value);
                }
            }
            var result = FilterEntities(command, entities, messageContext);
            return result;
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            var keys = command.ids;
            foreach (var key in keys) {
                var id              = key.AsString();
                var partitionKey    = new PartitionKey(id);
                // todo handle error;
                await cosmosContainer.DeleteItemStreamAsync(id, partitionKey);
            }
            var result = new DeleteEntitiesResult();
            return result;
        }
    }
}

#endif