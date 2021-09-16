// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    /// <summary>
    /// An <see cref="EntityContainer"/> is the abstraction of a collection / table used to store their entities / records 
    /// as key value pairs. It uses a string as key and a JSON object as value. Each container is intended to store the
    /// entities / records of a specific type. E.g. one container for storing JSON objects representing 'articles'
    /// another one for storing 'orders'.
    /// <para>
    ///   <see cref="EntityContainer"/> define the entire set of interfaces a database adapter needs to implement to
    ///   enable the complete feature set of <see cref="Graph.EntitySet{TKey,T}"/> and <see cref="Graph.EntityStore"/>.
    ///   <see cref="EntityContainer"/> and all its implementations must be thread safe.
    /// </para>
    /// <para>
    ///   The interface methods are designed to enable clear, compact and efficient implementations of database
    ///   operations. E.g. operations like SELECT, INSERT, DELETE or UPDATE in case of an SQL database adapter.
    ///   <see cref="MemoryContainer"/>, <see cref="FileContainer"/> and <c>CosmosContainer</c> show straight forward
    ///   implementation of <see cref="EntityContainer"/>.
    ///   Additional to memory implementation <see cref="FileContainer"/> shows also how to handle database errors.
    ///   These errors fall into two categories:
    ///   <para>1. A complete database request fails. E.g. a SELECT in SQL.
    ///         => <see cref="ICommandResult.Error"/> need to be set.
    ///   </para> 
    ///   <para>2. The database request was successful, but one or more entities (key/values) had an error when accessing.
    ///         E.g. Writing an entity to a file with a <see cref="FileContainer"/> fails because it is used by another process.
    ///         => An <see cref="EntityError"/> need to be added to entity error dictionary of the <see cref="ICommandResult"/>
    ///            E.g. an error is added to <see cref="CreateEntitiesResult.createErrors"/> in case of
    ///            <see cref="FileContainer.CreateEntities"/>
    ///   </para>
    ///   
    ///   All ...Result types returned by the interface methods of <see cref="EntityContainer"/> like
    ///   <see cref="CreateEntities"/>, <see cref="ReadEntities"/>, ... implement <see cref="ICommandResult"/>.
    ///   In case a database command fails completely  <see cref="ICommandResult.Error"/> needs to be set.
    ///   See <see cref="EntityDatabase.ExecuteSync"/> for proper error handling.
    /// </para>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class EntityContainer : IDisposable
    {
        public  readonly    string          name;
        private readonly    EntityDatabase  database;

        public  virtual     bool            Pretty      => false;

        public abstract Task<CreateEntitiesResult>  CreateEntities  (CreateEntities command, MessageContext messageContext);
        public abstract Task<UpsertEntitiesResult>  UpsertEntities  (UpsertEntities command, MessageContext messageContext);
        public abstract Task<ReadEntitiesResult>    ReadEntities    (ReadEntities   command, MessageContext messageContext);
        public abstract Task<QueryEntitiesResult>   QueryEntities   (QueryEntities  command, MessageContext messageContext);
        public abstract Task<DeleteEntitiesResult>  DeleteEntities  (DeleteEntities command, MessageContext messageContext);


        protected EntityContainer(string name, EntityDatabase database) {
            this.name = name;
            database.AddContainer(this);
            this.database = database;
        }
        
        public virtual  void                        Dispose() { }

        /// <summary>
        /// Default implementation to apply patches to entities.
        /// The implementation perform three steps:
        /// 1. Read entities to be patches from a database
        /// 2. Apply patches
        /// 3. Write back the patched entities
        ///
        /// If the used database has integrated support for patching JSON its <see cref="EntityContainer"/>
        /// implementation can override this method to replace two database requests by one.
        /// </summary>
        public virtual async Task<PatchEntitiesResult> PatchEntities   (PatchEntities patchEntities, SyncResponse response, MessageContext messageContext) {
            var entityPatches = patchEntities.patches;
            var ids = entityPatches.Select(patch => patch.Key).ToHashSet(JsonKey.Equality);
            // Read entities to be patched
            var readTask = new ReadEntities { ids = ids };
            var readResult = await ReadEntities(readTask, messageContext).ConfigureAwait(false);
            if (readResult.Error != null) {
                return new PatchEntitiesResult { Error = readResult.Error };
            }
            var entities = readResult.entities;
            if (entities.Count != ids.Count)
                throw new InvalidOperationException($"PatchEntities: Expect entities.Count of response matches request. expect: {ids.Count} got: {entities.Count}");
            
            // Apply patches
            // targets collect entities with: successful read & successful applied patch 
            var targets     = new  List<JsonValue>  (entities.Count);
            var targetKeys  = new  List<JsonKey>    (entities.Count);
            var container   = patchEntities.container;
            Dictionary<JsonKey, EntityError> patchErrors = null;
            using (var pooledPatcher = messageContext.pools.JsonPatcher.Get()) {
                JsonPatcher patcher = pooledPatcher.instance;
                foreach (var entity in entities) {
                    var key = entity.Key;
                    if (!ids.Contains(key))
                        throw new InvalidOperationException($"PatchEntities: Unexpected key in ReadEntities response: key: {key}");
                    var patch = entityPatches[key];
                    var value = entity.Value;
                    var error = value.Error; 
                    if (error != null) {
                        AddEntityError(ref patchErrors, key, error);
                        continue;
                    }
                    string target = value.Json;
                    if (target == null) {
                        error = new EntityError(EntityErrorType.PatchError, container, key, "patch target not found");
                        AddEntityError(ref patchErrors, key, error);
                        continue;
                    }
                    var json = patcher.ApplyPatches(target, patch.patches, Pretty);
                    entity.Value.SetJson(json);
                    targets.Add(new JsonValue(json));
                    targetKeys.Add(key);
                }
            }
            var valError = database.schema?.ValidateEntities(container, targetKeys, targets, messageContext, EntityErrorType.PatchError, ref response.patchErrors);
            if (valError != null) {
                return new PatchEntitiesResult{Error = new CommandError(valError)};
            }
            
            // Write patched entities back
            var task = new UpsertEntities {entities = targets, entityKeys = targetKeys };
            var upsertResult = await UpsertEntities(task, messageContext).ConfigureAwait(false);
            if (upsertResult.Error != null) {
                return new PatchEntitiesResult {Error = upsertResult.Error};
            }
            var upsertErrors = upsertResult.upsertErrors;
            if (upsertErrors != null) {
                foreach (var errorEntry in upsertErrors) {
                    var key = errorEntry.Key;
                    var error = errorEntry.Value;
                    AddEntityError(ref patchErrors, key, error);
                }
            }
            return new PatchEntitiesResult{patchErrors = patchErrors};
        }
        
        protected async Task<QueryEntitiesResult> FilterEntityIds(QueryEntities command, HashSet<JsonKey> ids, MessageContext messageContext) {
            var readIds         = new ReadEntities {ids = ids};
            var readEntities    = await ReadEntities(readIds, messageContext).ConfigureAwait(false);
            if (readEntities.Error != null) {
                // todo add error test 
                var message = $"failed filter entities of '{name}' (filter: {command.filterLinq}) - {readEntities.Error.message}";
                var error = new CommandError (message);
                return new QueryEntitiesResult {Error = error};
            }
            var result = FilterEntities(command, readEntities.entities, messageContext);
            return result;
        }
        
        protected QueryEntitiesResult FilterEntities(QueryEntities command, Dictionary <JsonKey, EntityValue> entities, MessageContext messageContext) {
            var jsonFilter      = new JsonFilter(command.filter); // filter can be reused
            var result          = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
            using (var pooledEvaluator = messageContext.pools.JsonEvaluator.Get()) {
                JsonEvaluator evaluator = pooledEvaluator.instance;
                foreach (var entityPair in entities) {
                    var key     = entityPair.Key;
                    var payload = entityPair.Value.Json;
                    if (!evaluator.Filter(payload, jsonFilter))
                        continue;
                    var entry = new EntityValue(payload);
                    result.Add(key, entry);
                }
            }
            return new QueryEntitiesResult{entities = result};
        }
        
        private static List<ReferencesResult> GetReferences(
            List<References>                    references,
            Dictionary<JsonKey, EntityValue>    entities,
            string                              container,
            MessageContext                      messageContext)
        {
            if (references.Count == 0)
                throw new InvalidOperationException("Expect references.Count > 0");
            var referenceResults = new List<ReferencesResult>(references.Count);
            
            // prepare single ScalarSelect and references results
            var selectors = new List<string>(references.Count);  // can be reused
            foreach (var reference in references) {
                selectors.Add(reference.selector);
                var referenceResult = new ReferencesResult {
                    container   = reference.container,
                    ids         = new HashSet<JsonKey>(JsonKey.Equality)
                };
                referenceResults.Add(referenceResult);
            }
            var select      = new ScalarSelect(selectors);  // can be reused
            using (var pooledSelector    = messageContext.pools.ScalarSelector.Get()) {
                ScalarSelector selector = pooledSelector.instance;
                // Get the selected refs for all entities.
                // Select() is expensive as it requires a full JSON parse. By using an selector array only one
                // parsing cycle is required. Otherwise for each selector Select() needs to be called individually.
                foreach (var entityPair in entities) {
                    EntityValue entity  = entityPair.Value;
                    if (entity.Error != null)
                        continue;
                    var         json    = entity.Json;
                    if (json == null)
                        continue;
                    var selectorResults = selector.Select(json, select);
                    if (selectorResults == null) {
                        var error = new EntityError(EntityErrorType.ParseError, container, entityPair.Key, selector.ErrorMessage);
                        entity.SetError(error);
                        continue;
                    }
                    for (int n = 0; n < references.Count; n++) {
                        // selectorResults[n] contains Select() result of selectors[n] 
                        var entityRefs = selectorResults[n].AsJsonKeys();
                        var referenceResult = referenceResults[n];
                        referenceResult.ids.UnionWith(entityRefs);  // TAG_PERF (count & combine)
                    }
                }
            }
            return referenceResults;
        }

        public async Task<ReadReferencesResult> ReadReferences(
                List<References>                    references,
                Dictionary<JsonKey, EntityValue>    entities,
                string                              container,
                string                              selectorPath,
                SyncResponse                        syncResponse,
                MessageContext                      messageContext)
        {
            var referenceResults = GetReferences(references, entities, container, messageContext);
            
            // add referenced entities to ContainerEntities
            for (int n = 0; n < references.Count; n++) {
                var reference       = references[n];
                var refContName     = reference.container;
                var refCont         = database.GetOrCreateContainer(refContName);
                var referenceResult = referenceResults[n];
                var ids = referenceResult.ids;
                if (ids.Count == 0)
                    continue;
                var refIdList   = ids;
                var readRefIds  = new ReadEntities {ids = refIdList};
                var refEntities = await refCont.ReadEntities(readRefIds, messageContext).ConfigureAwait(false);
                var subPath = $"{selectorPath} -> {reference.selector}";
                // In case of ReadEntities error: Assign error to result and continue with other references.
                // Resolving other references are independent may be successful.
                if (refEntities.Error != null) {
                    var message = $"read references failed: '{container}{subPath}' - {refEntities.Error.message}";
                    referenceResult.error = message;
                    continue;
                }
                var containerResult = syncResponse.GetContainerResult(refContName);
                containerResult.AddEntities(refEntities.entities);
                var subReferences = reference.references;  
                
                if (subReferences == null)
                    continue;
                var subEntities = new Dictionary<JsonKey, EntityValue>(ids.Count, JsonKey.Equality);
                foreach (var id in ids) {
                    subEntities.Add(id, refEntities.entities[id]);
                }
                var refReferencesResult =
                    await ReadReferences(subReferences, subEntities, refContName, subPath, syncResponse, messageContext).ConfigureAwait(false);
                // returned refReferencesResult.references is always set. Each references[] item contain either a result or an error.
                referenceResult.references = refReferencesResult.references;
            }
            return new ReadReferencesResult {references = referenceResults};
        }

        protected static void AddEntityError(ref Dictionary<JsonKey, EntityError> errors, JsonKey key, EntityError error) {
            if (errors == null) {
                errors = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
            }
            // add with TryAdd(). Only the first entity error is relevant. Subsequent entity errors are consequential failures.
            errors.TryAdd(key, error);
        }
        
        // may move to more appropriate class
        public static List<JsonKey> CreateEntityKeys (
            string                                  keyName,
            List<JsonValue>                         entities,
            MessageContext                          messageContext,
            out string                              error
        ) {
            keyName = keyName ?? "id";
            var keys = new List<JsonKey>(entities.Count);
            using (var poolValidator = messageContext.pools.EntityValidator.Get()) {
                var validator = poolValidator.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    if (validator.GetEntityKey(entity.json, keyName, out JsonKey key, out string entityError)) {
                        keys.Add(key);
                        continue;
                    }
                    error = $"error at entities[{n}]: {entityError}";
                    return null;
                }
            }
            AssertEntityCounts(keys, entities);
            error = null;
            return keys;
        }
        
        [Conditional("DEBUG")]
        public static void AssertEntityCounts(List<JsonKey> keys, List<JsonValue> entities) {
            if (keys.Count != entities.Count)
                throw new InvalidOperationException("expect equal counts");
        }
    }

    /// <see cref="ReadReferencesResult"/> is never serialized within a <see cref="SyncResponse"/> only its
    /// fields <see cref="references"/>.
    public class ReadReferencesResult
    {
        internal List<ReferencesResult> references;
    } 
}
