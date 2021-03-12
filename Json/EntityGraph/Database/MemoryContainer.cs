// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Friflo.Json.EntityGraph.Database
{
    public class MemoryDatabase : EntityDatabase
    {
        protected override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database);
        }
    }
    
    public class MemoryContainer : EntityContainer
    {
        private readonly Dictionary<string, string>     payloads    = new Dictionary<string, string>();

        public MemoryContainer(string name, EntityDatabase database) : base (name, database) { }


#pragma warning disable 1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await TaskEx.Run(...)' to do CPU-bound work on a background thread
        public override async Task CreateEntities(ICollection<KeyValue> entities) {
            foreach (var entity in entities) {
                payloads[entity.key] = entity.value;
            }
        }

        public override async Task UpdateEntities(ICollection<KeyValue> entities) {
            foreach (var entity in entities) {
                if (!payloads.TryGetValue(entity.key, out string _))
                    throw new InvalidOperationException($"Expect Entity with id {entity.key} in DatabaseContainer: {name}");
                payloads[entity.key] = entity.value;
            }
        }

        public override async Task<ICollection<KeyValue>> ReadEntities(ICollection<string> ids) {
            var result = new List<KeyValue>();
            foreach (var id in ids) {
                payloads.TryGetValue(id, out var payload);
                var entry = new KeyValue {
                    key     = id,
                    value   = payload
                };
                result.Add(entry);
            }
            return result;
        }
#pragma warning restore 1998
    }
}