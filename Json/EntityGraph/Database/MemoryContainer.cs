// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst; // UnityExtension.TryAdd()

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


        public override void CreateEntities(Dictionary<string, EntityValue> entities) {
            foreach (var entity in entities) {
                payloads[entity.Key] = entity.Value.value.json;
            }
        }

        public override void UpdateEntities(Dictionary<string, EntityValue> entities) {
            foreach (var entity in entities) {
                if (!payloads.TryGetValue(entity.Key, out string _))
                    throw new InvalidOperationException($"Expect Entity with id {entity.Key} in DatabaseContainer: {name}");
                payloads[entity.Key] = entity.Value.value.json;
            }
        }

        public override Dictionary<string, EntityValue> ReadEntities(ICollection<string> ids) {
            var result = new Dictionary<string, EntityValue>();
            foreach (var id in ids) {
                payloads.TryGetValue(id, out var payload);
                var entry = new EntityValue(payload);
                result.TryAdd(id, entry);
            }
            return result;
        }
    }
}