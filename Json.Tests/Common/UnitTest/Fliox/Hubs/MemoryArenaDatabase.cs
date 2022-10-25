// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    /// <summary>
    /// skeleton prototype only <br/>
    /// 
    /// The intention of this memory database is to reduce the number of heap allocations required by a <see cref="MemoryDatabase"/>.
    /// The <see cref="MemoryDatabase"/> allocates values as byte arrays and keys as strings on the heap.
    /// Note: Key strings are used only, if the <see cref="JsonKey"/>s are not of type long or GUID.
    /// The strings and byte arrays are likely to move to Generation 2.
    /// Doing so put pressure on GC when collecting garbage of Generation 2 objects.
    /// <br/>
    /// <see cref="MemoryArenaContainer"/> instead is intended to store multiple keys and values into a continuous byte array.
    /// This reduces the number of heap allocation significant and also enables higher memory locality when iterating key values.
    /// <br/>
    /// As key values can be deleted or modified the arena arrays will fragment over time so that a compaction phase is required
    /// to remove the fragmentation 'holes'.
    /// </summary>
    public sealed class MemoryArenaDatabase : EntityDatabase
    {
        private  readonly   bool                pretty;
        public   override   string              StorageType => "memory-arena";

        public MemoryArenaDatabase(string dbName, DatabaseService service = null, DbOpt opt = null, bool pretty = false)
            : base(dbName, service, opt)
        {
            this.pretty     = pretty;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryArenaContainer(name, database, pretty);
        }
    }
    
    
    public sealed class MemoryArenaContainer : EntityContainer
    {
        internal  readonly   ConcurrentDictionary<JsonKey, ArenaEntry>  keyValues;
        
        public   override   bool                                        Pretty      { get; }
        
        public    override  string  ToString()  => $"{name}  Count: {keyValues.Count}";

        public MemoryArenaContainer(string name, EntityDatabase database, bool pretty)
            : base(name, database)
        {
            keyValues = new ConcurrentDictionary<JsonKey, ArenaEntry>(JsonKey.Equality);
            Pretty = pretty;
        }
        
        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            List<EntityError> createErrors = null;
            for (int n = 0; n < entities.Count; n++) {
                var entity  = entities[n];
                var key     = entity.key;
                var entry   = CreateEntry(key, entity.value);
                if (keyValues.TryAdd(key, entry))
                    continue;
                var error = new EntityError(EntityErrorType.WriteError, name, key, "entity already exist");
                AddEntityError(ref createErrors, key, error);
            }
            var result = new CreateEntitiesResult { errors = createErrors };
            return Task.FromResult(result);
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            var entities = command.entities;
            for (int n = 0; n < entities.Count; n++) {
                var entity  = entities[n];
                var key     = entity.key;
                keyValues[key] = CreateEntry(key, entity.value);
            }
            var result = new UpsertEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            var keys = command.ids;
            var entities = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            foreach (var key in keys) {
                keyValues.TryGetValue(key, out var arenaEntry);
                var payload = GetValue(arenaEntry.index);
                var entry   = new EntityValue(payload);
                entities.TryAdd(key, entry);
            }
            var result = new ReadEntitiesResult{entities = entities};
            return Task.FromResult(result);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            if (!FindCursor(command.cursor, syncContext, out var keyValueEnum, out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            keyValueEnum = keyValueEnum ?? new MemoryArenaQueryEnumerator(this);
            try {
                var result  = await FilterEntities(command, keyValueEnum, syncContext).ConfigureAwait(false);
                return result;
            }
            finally {
                keyValueEnum.Dispose();
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            var filter = command.GetFilter();
            switch (command.type) {
                case AggregateType.count:
                    // count all?
                    if (filter.IsTrue) {
                        var count = keyValues.Count;
                        return new AggregateEntitiesResult { container = command.container, value = count };
                    }
                    var result = await CountEntities(command, syncContext).ConfigureAwait(false);
                    return result;
            }
            return new AggregateEntitiesResult { Error = new CommandError($"aggregate {command.type} not implement") };
        }

        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var keys = command.ids;
            if (keys != null && keys.Count > 0) {
                foreach (var key in keys) {
                    keyValues.TryRemove(key, out _);
                }
            }
            var all = command.all;
            if (all != null && all.Value) {
                keyValues.Clear();
            }
            var result = new DeleteEntitiesResult();
            return Task.FromResult(result);
        }
        
        // ------------------------------------- memory-arena -------------------------------------
        internal    byte[]  arenaBuffer = new byte[0];
        internal    int     arenaIndex  = 0;
        
        private ArenaEntry CreateEntry (in JsonKey key, in JsonValue value) {
            if (key.Type == JsonKeyType.String) {
                var keyStr  = key.AsString();
                var keyLen  = Encoding.UTF8.GetByteCount(keyStr);
                var len     = Encoding.UTF8.GetBytes(keyStr, 0, keyStr.Length, arenaBuffer, arenaIndex);
                Debug.Assert(keyLen == len);
            }
            return new ArenaEntry();
        }
        
        internal JsonValue GetValue (uint index) {
            return new JsonValue();
        }
    }
    
    internal readonly struct ArenaEntry {
        internal readonly    uint     index;
        
        internal ArenaEntry(uint index) {
            this.index = index;
        }
    }
    
    internal class MemoryArenaQueryEnumerator : QueryEnumerator
    {
        private readonly MemoryArenaContainer                           container;
        private readonly IEnumerator<KeyValuePair<JsonKey, ArenaEntry>> enumerator;
        
        internal MemoryArenaQueryEnumerator(MemoryArenaContainer container) {
            this.container  = container;
            enumerator      = container.keyValues.GetEnumerator();
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
        public override JsonValue       CurrentValue        => container.GetValue(enumerator.Current.Value.index);
        public override Task<JsonValue> CurrentValueAsync() => throw new NotImplementedException();
    }
}