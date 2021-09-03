// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Database;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Sync;
using Microsoft.Azure.Cosmos;

namespace Friflo.Json.Fliox.Db.Cosmos
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
            var container = cosmosDatabase.CreateContainerIfNotExistsAsync(name, "/LastName", 400).Result; // todo make CreateContainer async
            return new CosmosContainer(name, database, container, pretty);
        }
    }
    
    public class CosmosContainer : EntityContainer
    {
        private  readonly   Container   cosmosContainer;
        public   override   bool        Pretty      { get; }

        public CosmosContainer(string name, EntityDatabase database, Container container, bool pretty) : base(name, database) {
            cosmosContainer = container;
            Pretty          = pretty;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            using(MemoryStream memory   = new MemoryStream())
            using(StreamWriter writer   = new StreamWriter(memory, Encoding.UTF8)) {
                foreach (var entityPair in entities) {
                    var         id      = entityPair.Key.AsString();
                    EntityValue payload  = entityPair.Value;
                    writer.Write(payload);
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
            using(MemoryStream memory   = new MemoryStream())
            using(StreamWriter writer   = new StreamWriter(memory, Encoding.UTF8)) {
                foreach (var entityPair in entities) {
                    var         key      = entityPair.Key.AsString();
                    EntityValue payload  = entityPair.Value;
                    writer.Write(payload);
                    memory.Seek(0, SeekOrigin.Begin);
                    var partitionKey = new PartitionKey(key);
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
                using (StreamReader reader = new StreamReader(response.Content)) {
                    string payload = await reader.ReadToEndAsync();
                    var entry = new EntityValue(payload);
                    entities.TryAdd(key, entry);
                }
            }
            var result = new ReadEntitiesResult{entities = entities};
            return result;
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            // var ids     = keyValues.Keys.ToHashSet(JsonKey.Equality);
            // var result  = await FilterEntities(command, ids, messageContext).ConfigureAwait(false);
            return null;
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