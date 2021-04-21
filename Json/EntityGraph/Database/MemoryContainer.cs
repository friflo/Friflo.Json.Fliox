// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Burst;  // UnityExtension.TryAdd()
using Friflo.Json.Flow.Graph; 

namespace Friflo.Json.EntityGraph.Database
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
        public  override    SyncContext     SyncContext { get; }

        public MemoryContainer(string name, EntityDatabase database, bool pretty) : base(name, database) {
            SyncContext = new SyncContext();
            Pretty = pretty;
        }
        
        public override void Dispose() {
            SyncContext.Dispose();
        }


        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities task) {
            var entities = task.entities;
            foreach (var entity in entities) {
                payloads[entity.Key] = entity.Value.value.json;
            }
            return new CreateEntitiesResult();
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities task) {
            var entities = task.entities;
            foreach (var entity in entities) {
                if (!payloads.TryGetValue(entity.Key, out string _))
                    throw new InvalidOperationException($"Expect Entity with id {entity.Key} in DatabaseContainer: {name}");
                payloads[entity.Key] = entity.Value.value.json;
            }
            return new UpdateEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities task) {
            var ids = task.ids;
            var entities = new Dictionary<string, EntityValue>(ids.Count);
            foreach (var id in ids) {
                payloads.TryGetValue(id, out var payload);
                var entry = new EntityValue(payload);
                entities.TryAdd(id, entry);
            }
            return new ReadEntitiesResult{entities = entities};
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities task) {
            var result      = new Dictionary<string, EntityValue>();
            var jsonFilter  = new JsonFilter(task.filter); // filter can be reused
            foreach (var payloadPair in payloads) {
                var payload = payloadPair.Value;
                if (SyncContext.jsonEvaluator.Filter(payload, jsonFilter)) {
                    var entry = new EntityValue(payload);
                    result.Add(payloadPair.Key, entry);
                }
            }
            return new QueryEntitiesResult{ entities = result };
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities task) {
            var ids = task.ids;
            foreach (var id in ids) {
                payloads.Remove(id);
            }
            return new DeleteEntitiesResult();
        }

    }
}