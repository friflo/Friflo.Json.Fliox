// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Burst;  // UnityExtension.TryAdd(), ToHashSet()
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database
{
    /// <summary>
    /// <para>
    ///   EntityContainer define the entire set of interfaces a database adapter needs to implement to enable
    ///   the complete feature set of <see cref="Graph.EntitySet{T}"/> and <see cref="Graph.EntityStore"/>
    /// </para>
    /// <para>
    ///   The interface methods are designed to enable clear, compact and efficient implementations of database
    ///   requests. E.g. operations like SELECT, INSERT, DELETE or UPDATE in case of an SQL database adapter.
    ///   <see cref="MemoryContainer"/> and <see cref="FileContainer"/> show straight forward implementation of
    ///   <see cref="EntityContainer"/>. 
    ///   
    ///   All ...Result types returned by the interface methods of <see cref="EntityContainer"/> like
    ///   <see cref="CreateEntities"/>, <see cref="ReadEntities"/>, ... implement <see cref="IDatabaseResult"/>.
    ///   In case a database command fails completely  <see cref="IDatabaseResult.Error"/> needs to be set.
    /// </para>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class EntityContainer : IDisposable
    {
        public  readonly    string          name;
        private readonly    EntityDatabase  database;

        public virtual      bool            Pretty      => false;
        public virtual      SyncContext     SyncContext => null;

        public abstract Task<CreateEntitiesResult>  CreateEntities  (CreateEntities task);
        public abstract Task<UpdateEntitiesResult>  UpdateEntities  (UpdateEntities task);
        public abstract Task<ReadEntitiesResult>    ReadEntities    (ReadEntities   task);
        public abstract Task<QueryEntitiesResult>   QueryEntities   (QueryEntities  task);
        public abstract Task<DeleteEntitiesResult>  DeleteEntities  (DeleteEntities task);
        
        
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
        public virtual async Task<PatchEntitiesResult>      PatchEntities   (PatchEntities patchEntities) {
            var entityPatches = patchEntities.patches;
            var ids = entityPatches.Select(patch => patch.Key).ToHashSet();
            // Read entities to be patched
            var readTask = new ReadEntities {ids = ids};
            var readResult = await ReadEntities(readTask);
            var entities = readResult.entities;
            if (entities.Count != ids.Count)
                throw new InvalidOperationException($"PatchEntities: Expect entities.Count of response matches request. expect: {ids.Count} got: {entities.Count}");
            
            // Apply patches
            var patcher = SyncContext.jsonPatcher;
            foreach (var entity in entities) {
                var key = entity.Key;
                if (!ids.Contains(key))
                    throw new InvalidOperationException($"PatchEntities: Unexpected key in ReadEntities response: key: {key}");
                var patch = entityPatches[key];
                entity.Value.SetJson(patcher.ApplyPatches(entity.Value.Json, patch.patches, Pretty));
            }
            // Write patched entities back
            var task = new CreateEntities {entities = entities};
            await CreateEntities(task); // todo should be UpdateEntities
            return new PatchEntitiesResult();
        }

        public async Task<List<ReferencesResult>> ReadReferences(
                List<References>                    references,
                Dictionary<string, EntityValue>     entities,
                string                              container,
                SyncResponse                        syncResponse)
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
                    ids         = new HashSet<string>()
                };
                referenceResults.Add(referenceResult);
            }
            var select      = new ScalarSelect(selectors);  // can be reused
            var jsonPath    = SyncContext.scalarSelector;
            
            // Get the selected refs for all entities.
            // Select() is expensive as it requires a full JSON parse. By using an selector array only one
            // parsing cycle is required. Otherwise for each selector Select() needs to be called individually.
            foreach (var entityPair in entities) {
                EntityValue entity  = entityPair.Value;
                if (entity.Error != null)
                    continue;
                var         json    = entity.Json;
                if (json != null) {
                    var selectorResults = jsonPath.Select(json, select);
                    if (selectorResults == null) {
                        var error = new EntityError(EntityErrorType.ParseError, container, entityPair.Key, jsonPath.ErrorMessage);
                        entity.SetError(error);
                        continue;
                    }
                    for (int n = 0; n < references.Count; n++) {
                        // selectorResults[n] contains Select() result of selectors[n] 
                        var entityRefs = selectorResults[n].AsStrings();
                        var referenceResult = referenceResults[n];
                        referenceResult.ids.UnionWith(entityRefs);
                    }
                }
            }
            
            // add referenced entities to ContainerEntities
            for (int n = 0; n < references.Count; n++) {
                var reference       = references[n];
                var refContainer    = database.GetOrCreateContainer(reference.container);
                var referenceResult = referenceResults[n];
                var ids = referenceResult.ids;
                if (ids.Count > 0) {
                    var refIdList   = ids.ToHashSet();
                    var readRefIds  = new ReadEntities {ids = refIdList};
                    var refEntities = await refContainer.ReadEntities(readRefIds);
                    var containerResult = syncResponse.GetContainerResult(reference.container);
                    containerResult.AddEntities(refEntities.entities);
                    var subReferences = reference.references;  
                    if (subReferences != null) {
                        var subEntities = new Dictionary<string, EntityValue>(ids.Count);
                        foreach (var id in ids) {
                            subEntities.Add(id, refEntities.entities[id]);
                        }
                        referenceResult.references = await ReadReferences(subReferences, subEntities, reference.container, syncResponse); 
                    }
                }
            }
            return referenceResults;
        }
    }
}
