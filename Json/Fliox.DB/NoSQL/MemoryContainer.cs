// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    public class MemoryDatabase : EntityDatabase
    {
        private  readonly   bool    pretty;

        public MemoryDatabase(bool pretty = false) {
            this.pretty = pretty;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, pretty);
        }
    }
    
    public class MemoryContainer : EntityContainer
    {
        private  readonly   Dictionary<JsonKey, string> keyValues = new Dictionary<JsonKey, string>(JsonKey.Equality);
        
        public   override   bool                        Pretty      { get; }

        public MemoryContainer(string name, EntityDatabase database, bool pretty) : base(name, database) {
            Pretty = pretty;
        }
        
        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            foreach (var entityPair in entities) {
                var      key      = entityPair.Key;
                EntityValue payload  = entityPair.Value;
                if (keyValues.TryGetValue(key, out string _))
                    throw new InvalidOperationException($"Entity with key '{key}' already in DatabaseContainer: {name}");
                keyValues[key] = payload.Json;
            }
            var result = new CreateEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            var entities = command.entities;
            foreach (var entityPair in entities) {
                var         key      = entityPair.Key;
                EntityValue payload  = entityPair.Value;
                keyValues[key] = payload.Json;
            }
            var result = new UpsertEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            var keys = command.ids;
            var entities = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            foreach (var key in keys) {
                keyValues.TryGetValue(key, out var payload);
                var entry = new EntityValue(payload);
                entities.TryAdd(key, entry);
            }
            var result = new ReadEntitiesResult{entities = entities};
            return Task.FromResult(result);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            var ids     = keyValues.Keys.ToHashSet(JsonKey.Equality);  // TAG_PERF
            var result  = await FilterEntityIds(command, ids, messageContext).ConfigureAwait(false);
            return result;
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            var keys = command.ids;
            foreach (var key in keys) {
                keyValues.Remove(key);
            }
            var result = new DeleteEntitiesResult();
            return Task.FromResult(result);
        }
        
        public override Task<AutoIncrementResult>   AutoIncrement  (AutoIncrement  command, MessageContext messageContext) {
            throw new NotImplementedException("");
        }
    }
}