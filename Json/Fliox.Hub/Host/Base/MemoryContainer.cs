// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public sealed class MemoryContainer : EntityContainer
    {
        private  readonly   IDictionary<JsonKey, JsonValue>             keyValues;
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<JsonKey, JsonValue>    keyValuesConcurrent;
        private  readonly   int                                         smallValueSize;
        
        public   override   bool                                        Pretty      { get; }
        
        public    override  string  ToString()  => $"{name}  Count: {keyValues.Count}";

        public MemoryContainer(string name, EntityDatabase database, MemoryType type, bool pretty, int smallValueSize)
            : base(name, database)
        {
            this.smallValueSize     = smallValueSize;
            if (type == MemoryType.Concurrent) {
                keyValuesConcurrent = new ConcurrentDictionary<JsonKey, JsonValue>(JsonKey.Equality);
                keyValues           = keyValuesConcurrent;
            } else {
                keyValues           = new Dictionary<JsonKey, JsonValue>(JsonKey.Equality);
            }
            Pretty = pretty;
        }
        
        public override Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            return Task.FromResult(CreateEntities(command, syncContext));
        }
        
        public override CreateEntitiesResult CreateEntities(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            List<EntityError> createErrors = null;
            for (int n = 0; n < entities.Count; n++) {
                var entity  = entities[n];
                var key     = entity.key;
                // Add always a copy as it expect no key/value exist to update
                var value   = new JsonValue (entity.value);
                if (keyValues.TryAdd(key, value))
                    continue;
                var error = new EntityError(EntityErrorType.WriteError, nameShort, key, "entity already exist");
                AddEntityError(ref createErrors, key, error);
            }
            return new CreateEntitiesResult { errors = createErrors };
        }

        public override Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            return Task.FromResult(UpsertEntities(command, syncContext));           
        }
        
        public override UpsertEntitiesResult UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            var entities = command.entities;
            for (int n = 0; n < entities.Count; n++) {
                var entity              = entities[n];
                PutValue(entity);
            }
            return UpsertEntitiesResult.Create(syncContext, null);
        }

        public override Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
           return Task.FromResult(ReadEntities(command, syncContext));
        }
        
        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            var keys        = command.ids;
            var entities    = new EntityValue [keys.Count];
            int index       = 0;
            foreach (var key in keys) {
                TryGetValue(key, out JsonValue value, syncContext.MemoryBuffer);
                entities[index++]   = new EntityValue(key, value);
            }
            var result = new ReadEntitiesResult{entities = entities};
            return result;
        }
        
        public override Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            return Task.FromResult(QueryEntities(command, syncContext));
        }
        
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext) {
            if (!FindCursor(command.cursor, syncContext, out var keyValueEnum, out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            keyValueEnum        = keyValueEnum ?? new MemoryQueryEnumerator(keyValues);   // TAG_PERF
            var filterContext   = new EntityFilterContext(command, this, syncContext);
            try {
                return FilterEntities(filterContext, keyValueEnum, syncContext);
            } finally {
                filterContext.Dispose();
                keyValueEnum.Dispose();
            }
        }
        
        private QueryEntitiesResult FilterEntities (EntityFilterContext filterContext, QueryEnumerator keyValueEnum, SyncContext syncContext) {
            var result = new QueryEntitiesResult();
            while (keyValueEnum.MoveNext()) {
                var key     = keyValueEnum.Current;
                TryGetValue(key, out JsonValue value, syncContext.MemoryBuffer);
                if (value.IsNull())
                    continue;
                var filter  = filterContext.FilterEntity(key, value);
                
                if (filter == FilterEntityResult.FilterError)
                    return filterContext.QueryError(result);
                if (filter == FilterEntityResult.ReachedLimit)
                    break;
                if (filter == FilterEntityResult.ReachedMaxCount) {
                    result.cursor = StoreCursor(keyValueEnum, syncContext.User.userId);
                    break;
                }
            }
            result.entities = filterContext.Result.ToArray();
            return result;
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var filter = command.GetFilter();
            switch (command.type) {
                case AggregateType.count:
                    // count all?
                    if (filter.IsTrue) {
                        var count = keyValues.Count;
                        return new AggregateEntitiesResult { container = command.container, value = count };
                    }
                    var result = await CountEntitiesAsync(command, syncContext).ConfigureAwait(false);
                    return result;
            }
            return new AggregateEntitiesResult { Error = new CommandError($"aggregate {command.type} not implement") };
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            return Task.FromResult(DeleteEntities(command, syncContext));
        }

        public override DeleteEntitiesResult DeleteEntities(DeleteEntities command, SyncContext syncContext) {
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
            return new DeleteEntitiesResult();
        }
        
        public override Task<MergeEntitiesResult> MergeEntitiesAsync (MergeEntities mergeEntities, SyncContext syncContext) {
            return Task.FromResult(MergeEntities(mergeEntities, syncContext));
        }
        
        /// <summary> Optimized merge implementation specific for <see cref="MemoryContainer"/> </summary>
        public override MergeEntitiesResult MergeEntities (
            MergeEntities   mergeEntities,
            SyncContext     syncContext)
        {
            var patches                     = mergeEntities.patches;
            var validationType              = database.Schema?.GetValidationType(mergeEntities.container);
            var container                   = mergeEntities.container;
            List<EntityError> patchErrors   = null;
            using (var pooledMerger         = syncContext.pool.JsonMerger.Get())
            using (var pooledValidator      = syncContext.pool.TypeValidator.Get())
            {
                var merger      = pooledMerger.instance;
                var validator   = pooledValidator.instance;
                foreach (var patch in patches)
                {
                    if (!TryGetValue(patch.key, out var target, syncContext.MemoryBuffer)) {
                        var error = new EntityError(EntityErrorType.PatchError, container, patch.key, "patch target not found");
                        AddEntityError(ref patchErrors, patch.key, error);
                        continue;
                    }
                    if (target.IsNull()) {
                        var error = new EntityError(EntityErrorType.PatchError, container, patch.key, "patch target not found");
                        AddEntityError(ref patchErrors, patch.key, error);
                        continue;
                    }
                    // patch is an object - ensured by GetKeysFromEntities() above
                    var merge       = merger.Merge(target, patch.value); // todo use MergeBytes to avoid array copy
                    var mergeError  = merger.Error;
                    if (mergeError != null) {
                        var entityError = new EntityError(EntityErrorType.PatchError, container, patch.key, mergeError);
                        AddEntityError(ref patchErrors, patch.key, entityError);
                        continue;
                    }
                    if (validationType != null) {
                        if (!validator.ValidateObject(merge, validationType, out string error)) {
                            var entityError = new EntityError(EntityErrorType.PatchError, container, patch.key, error);
                            AddEntityError(ref patchErrors, patch.key, entityError);
                            continue;
                        }
                    }
                    var entity = new JsonEntity(patch.key, merge);
                    PutValue(entity);
                }
            }
            return new MergeEntitiesResult{ errors = patchErrors };
        }
        
        private void PutValue (in JsonEntity entity) {
            if (entity.value.IsNull()) {
                keyValues[entity.key] = default;
                return;
            }
            // Update if:   - current value exist
            //              - current and new values are small
            if (entity.value.Count <= smallValueSize) {
                if (keyValues.TryGetValue(entity.key, out var current)) {
                    if (!current.IsNull() && current.Count <= smallValueSize) {
                        lock (keyValues)  { // could use are more specific lock by using the entity value array instead
                            JsonValue.Copy(ref current, entity.value);
                            keyValues[entity.key] = current;
                        }
                        return;
                    }
                }
            }
            // Otherwise: put a value copy
            keyValues[entity.key] = new JsonValue(entity.value);
        }
        
        private bool TryGetValue(in JsonKey key, out JsonValue value, MemoryBuffer buffer) {
            if (!keyValues.TryGetValue(key, out value))
                return false;
            if (value.IsNull())
                return default;
            // If value is small:   return a copy of the value 
            if (value.Count <= smallValueSize) {
                lock (keyValues)  { // could lock its array instead
                    value = buffer.Add(value);
                    return true;
                }
            }
            // Otherwise:           return value as it is
            return true;
        }
    }
}