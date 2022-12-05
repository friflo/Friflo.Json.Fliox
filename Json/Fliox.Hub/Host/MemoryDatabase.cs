// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="MemoryDatabase"/> is a non-persistent database used to store records in memory.
    /// </summary>
    /// <remarks>
    /// The intention is having a shared database which can be used in high performance scenarios. <br/>
    /// E.g. on a 4 Core CPU it is able to achieve more than 500.000 request / second. <br/>
    /// Following use-cases are suitable for a <see cref="MemoryDatabase"/>
    /// <list type="bullet">
    ///   <item>Run a big amount of unit tests fast and efficient as instantiation of <see cref="MemoryDatabase"/> take only some micro seconds. </item>
    ///   <item>Use as a Game Session database for online multiplayer games as it provide sub millisecond response latency</item>
    ///   <item>Use as test database for <b>TDD</b> without any configuration </item>
    ///   <item>Is the benchmark reference for all other database implementations regarding throughput and latency</item>
    /// </list>
    /// <see cref="MemoryDatabase"/> has no third party dependencies.
    /// <i>Storage characteristics</i> <br/>
    /// <b>Keys</b> are stored as <see cref="JsonKey"/> - keys that can be converted to <see cref="long"/> or <see cref="Guid"/>
    /// are stored without heap allocation. Otherwise a <see cref="string"/> is allocated <br/>
    /// <b>Values</b> are stored as <see cref="JsonValue"/> - essentially a <see cref="byte"/>[]
    /// </remarks>
    public sealed class MemoryDatabase : EntityDatabase
    {
        private  readonly   bool        pretty;
        private  readonly   MemoryType  containerType;
        private  readonly   int         smallValueSize;
        
        public   override   string      StorageType => "in-memory";

        /// <param name="dbName"></param>
        /// <param name="service"></param>
        /// <param name="type"></param>
        /// <param name="opt"></param>
        /// <param name="pretty"></param>
        /// <param name="smallValueSize"> Intended for write heavy containers. <br/>
        /// Byte arrays used to store container values are reused in case their length is less or equal this size. 
        /// </param>
        public MemoryDatabase(
            string          dbName,
            DatabaseService service         = null,
            MemoryType?     type            = null,
            DbOpt           opt             = null,
            bool            pretty          = false,
            int             smallValueSize  = -1)
            : base(dbName, service, opt)
        {
            this.pretty         = pretty;
            this.smallValueSize = smallValueSize;
            containerType       = type ?? MemoryType.Concurrent;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, containerType, pretty, smallValueSize);
        }
    }
    
    public enum MemoryType {
        Concurrent,
        /// used to preserve insertion order of entities in ClusterDB and MonitorDB
        NonConcurrent
    }
    
    public sealed class MemoryContainer : EntityContainer
    {
        private  readonly   IDictionary<JsonKey, JsonValue>             keyValues;
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
        
        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            return Task.FromResult(CreateEntitiesSync(command, syncContext));
        }
        
        public override CreateEntitiesResult CreateEntitiesSync(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            List<EntityError> createErrors = null;
            for (int n = 0; n < entities.Count; n++) {
                var entity  = entities[n];
                var key     = entity.key;
                // Add always a copy as it expect no key/value exist to update
                var value   = new JsonValue (entity.value);
                if (keyValues.TryAdd(key, value))
                    continue;
                var error = new EntityError(EntityErrorType.WriteError, name, key, "entity already exist");
                AddEntityError(ref createErrors, key, error);
            }
            return new CreateEntitiesResult { errors = createErrors };
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            return Task.FromResult(UpsertEntitiesSync(command, syncContext));           
        }
        
        public override UpsertEntitiesResult UpsertEntitiesSync(UpsertEntities command, SyncContext syncContext) {
            var entities = command.entities;
            for (int n = 0; n < entities.Count; n++) {
                var entity              = entities[n];
                PutValue(entity);
            }
            return new UpsertEntitiesResult();
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
           return Task.FromResult(ReadEntitiesSync(command, syncContext));
        }
        
        public override ReadEntitiesResult ReadEntitiesSync(ReadEntities command, SyncContext syncContext) {
            var keys        = command.ids;
            var entities    = new EntityValue [keys.Count];
            int index       = 0;
            foreach (var key in keys) {
                TryGetValue(key, out JsonValue value, syncContext.memoryBuffer);
                entities[index++]   = new EntityValue(key, value);
            }
            var result = new ReadEntitiesResult{entities = entities};
            return result;
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            return Task.FromResult(QueryEntitiesSync(command, syncContext));
        }
        
        public override QueryEntitiesResult QueryEntitiesSync(QueryEntities command, SyncContext syncContext) {
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
                TryGetValue(key, out JsonValue value, syncContext.memoryBuffer);
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
            return Task.FromResult(DeleteEntitiesSync(command, syncContext));
        }

        public override DeleteEntitiesResult DeleteEntitiesSync(DeleteEntities command, SyncContext syncContext) {
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
        
        public override Task<MergeEntitiesResult> MergeEntities (MergeEntities mergeEntities, SyncContext syncContext) {
            return Task.FromResult(MergeEntitiesSync(mergeEntities, syncContext));
        }
        
        /// <summary> Optimized implementation </summary>
        public MergeEntitiesResult MergeEntitiesSync (MergeEntities mergeEntities, SyncContext syncContext) {
            var patches = mergeEntities.patches;
            if (!EntityUtils.GetKeysFromEntities(mergeEntities.keyName, patches, syncContext, out string keyError)) {
                var error = new CommandError(TaskErrorResultType.InvalidTask, keyError);
                return new MergeEntitiesResult { Error = error };
            }
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
                    if (!TryGetValue(patch.key, out var target, syncContext.memoryBuffer)) {
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
    
    internal sealed class MemoryQueryEnumerator : QueryEnumerator
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
    }
}