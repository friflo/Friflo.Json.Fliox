// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public sealed class MemoryDatabase : EntityDatabase
    {
        private  readonly   bool                pretty;
        private  readonly   MemoryContainerType containerType;
        public   override   string              StorageName => "memory";

        public MemoryDatabase(TaskHandler handler = null, MemoryContainerType? type = null, DbOpt opt = null, bool pretty = false)
            : base(handler, opt)
        {
            this.pretty     = pretty;
            containerType   = type ?? MemoryContainerType.Concurrent;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, containerType, pretty);
        }
    }
    
    public enum MemoryContainerType {
        Concurrent,
        /// used to preserve insertion order of entities in ClusterDB and MonitorDB
        NonConcurrent
    }
    
    public sealed class MemoryContainer : EntityContainer
    {
        private  readonly   IDictionary<JsonKey, JsonValue>             keyValues;
        private  readonly   ConcurrentDictionary<JsonKey, JsonValue>    keyValuesConcurrent;
        
        public   override   bool                                        Pretty      { get; }
        
        public    override  string  ToString()  => $"{base.ToString()}, Count: {keyValues.Count}";

        public MemoryContainer(string name, EntityDatabase database, MemoryContainerType type, bool pretty)
            : base(name, database)
        {
            if (type == MemoryContainerType.Concurrent) {
                keyValuesConcurrent = new ConcurrentDictionary<JsonKey, JsonValue>(JsonKey.Equality);
                keyValues           = keyValuesConcurrent;
            } else {
                keyValues = new Dictionary<JsonKey, JsonValue>(JsonKey.Equality);
            }
            Pretty = pretty;
        }
        
        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            for (int n = 0; n < entities.Count; n++) {
                var key     = command.entityKeys[n];
                var payload = entities[n];
                if (keyValues.TryGetValue(key, out JsonValue _))
                    throw new InvalidOperationException($"Entity with key '{key}' already in DatabaseContainer: {name}");
                keyValues[key] = payload;
            }
            var result = new CreateEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            for (int n = 0; n < entities.Count; n++) {
                var key     = command.entityKeys[n];
                var payload = entities[n];
                keyValues[key] = payload;
            }
            var result = new UpsertEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<ReadEntitiesSetResult> ReadEntitiesSet(ReadEntitiesSet command, MessageContext messageContext) {
            var keys = command.ids;
            var entities = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            foreach (var key in keys) {
                keyValues.TryGetValue(key, out var payload);
                var entry = new EntityValue(payload);
                entities.TryAdd(key, entry);
            }
            var result = new ReadEntitiesSetResult{entities = entities};
            return Task.FromResult(result);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            if (!FindCursor(command.cursor, messageContext, out var keyValueEnum, out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            keyValueEnum = keyValueEnum ?? new MemoryQueryEnumerator(keyValues);   // TAG_PERF
            try {
                var result  = await FilterEntities(command, keyValueEnum, messageContext).ConfigureAwait(false);
                return result;
            }
            finally {
                keyValueEnum.Dispose();
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntities (AggregateEntities command, MessageContext messageContext) {
            var filter = command.GetFilter();
            switch (command.type) {
                case AggregateType.count:
                    // count all?
                    if (filter.IsTrue) {
                        var count = keyValues.Count;
                        return new AggregateEntitiesResult { container = command.container, value = count };
                    }
                    var result = await CountEntities(command, messageContext).ConfigureAwait(false);
                    return result;
            }
            return new AggregateEntitiesResult { Error = new CommandError($"aggregate {command.type} not implement") };
        }

        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            var keys = command.ids;
            if (keys != null && keys.Count > 0) {
                foreach (var key in keys) {
                    if (keyValuesConcurrent != null) {
                        keyValuesConcurrent.TryRemove(key, out _);
                        continue;
                    }
                    keyValues.Remove(key);
                }
            }
            var all = command.all;
            if (all != null && all.Value) {
                keyValues.Clear();
            }
            var result = new DeleteEntitiesResult();
            return Task.FromResult(result);
        }
    }
    
    internal class MemoryQueryEnumerator : QueryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<JsonKey, JsonValue>>  enumerator;
        
        internal MemoryQueryEnumerator(IDictionary<JsonKey, JsonValue> map) {
            enumerator = map.GetEnumerator();
        }

        public override bool MoveNext() {
            return enumerator.MoveNext();
        }

        public override JsonKey Current => enumerator.Current.Key;

        protected override void DisposeEnumerator() {
            enumerator.Dispose();
        }
        
        // --- ContainerEnumerator
        public override bool            IsAsync             => false;
        public override JsonValue       CurrentValue        => enumerator.Current.Value;
        public override Task<JsonValue> CurrentValueAsync() => throw new NotImplementedException();
    }
}