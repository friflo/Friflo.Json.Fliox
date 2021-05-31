// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class MemoryDatabase : EntityDatabase
    {
        private readonly    bool    pretty;

        public MemoryDatabase(bool pretty = false) {
            this.pretty = pretty;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, pretty);
        }
    }
    
    public class MemoryContainer : EntityContainer
    {
        private readonly    Dictionary<string, string>  payloads    = new Dictionary<string, string>();
        
        public  override    bool            Pretty      { get; }

        public MemoryContainer(string name, EntityDatabase database, bool pretty) : base(name, database) {
            Pretty = pretty;
        }
        
        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            foreach (var entityPair in entities) {
                string      key      = entityPair.Key;
                EntityValue payload  = entityPair.Value;
                payloads[key] = payload.Json;
            }
            var result = new CreateEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            foreach (var entityPair in entities) {
                string      key      = entityPair.Key;
                EntityValue payload  = entityPair.Value;
                if (!payloads.TryGetValue(key, out string _))
                    throw new InvalidOperationException($"Expect Entity with key {key} in DatabaseContainer: {name}");
                payloads[key] = payload.Json;
            }
            var result = new UpdateEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            var keys = command.ids;
            var entities = new Dictionary<string, EntityValue>(keys.Count);
            foreach (var key in keys) {
                payloads.TryGetValue(key, out var payload);
                var entry = new EntityValue(payload);
                entities.TryAdd(key, entry);
            }
            var result = new ReadEntitiesResult{entities = entities, entitiesValidated = true};
            return Task.FromResult(result);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            var ids     = payloads.Keys.ToHashSet();
            var result  = await FilterEntities(command, ids, syncContext);
            return result;
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var keys = command.ids;
            foreach (var key in keys) {
                payloads.Remove(key);
            }
            var result = new DeleteEntitiesResult();
            return Task.FromResult(result);
        }

    }
}