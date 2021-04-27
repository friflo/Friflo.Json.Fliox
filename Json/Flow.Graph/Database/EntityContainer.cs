// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Graph.Database
{
    public abstract class EntityContainer : IDisposable
    {
        public  readonly    string          name;
        private readonly    EntityDatabase  database;

        public virtual      bool            Pretty      => false;
        public virtual      SyncContext     SyncContext => null;


        protected EntityContainer(string name, EntityDatabase database) {
            this.name = name;
            database.AddContainer(this);
            this.database = database;
        }
        
        public virtual  void                            Dispose() { }
        
        public abstract Task<CreateEntitiesResult>  CreateEntities  (CreateEntities task);
        public abstract Task<UpdateEntitiesResult>  UpdateEntities  (UpdateEntities task);
        public abstract Task<ReadEntitiesResult>    ReadEntities    (ReadEntities task);
        public abstract Task<QueryEntitiesResult>   QueryEntities   (QueryEntities task);
        public abstract Task<DeleteEntitiesResult>  DeleteEntities  (DeleteEntities task);

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
            var entityPatches = patchEntities.entityPatches;
            var ids = entityPatches.Select(patch => patch.id).ToList();
            // Read entities to be patched
            var readTask = new ReadEntities {ids = ids};
            var readResult = await ReadEntities(readTask);
            var entities = readResult.entities;
            if (entities.Count != ids.Count)
                throw new InvalidOperationException($"PatchEntities: Expect entities.Count of response matches request. expect: {ids.Count} got: {entities.Count}");
            
            // Apply patches
            var patcher = SyncContext.jsonPatcher;
            int n = 0;
            foreach (var entity in entities) {
                var expectedId = ids[n];
                var patch = entityPatches[n++];
                if (entity.Key != expectedId) {
                    throw new InvalidOperationException($"PatchEntities: Expect entity key of response matches request: index:{n} expect: {expectedId} got: {entity.Key}");
                }
                entity.Value.value.json = patcher.ApplyPatches(entity.Value.value.json, patch.patches, Pretty);
            }
            // Write patched entities back
            var task = new CreateEntities {entities = entities};
            await CreateEntities(task); // should be UpdateEntities
            return new PatchEntitiesResult();
        }

        public async Task<List<ReferencesResult>> ReadReferences(
                List<References>                    references,
                Dictionary<string, EntityValue>     entities,
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
                var         json    = entity.value.json;
                if (json != null) {
                    var selectorResults = jsonPath.Select(json, select);
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
                var refContainer    = database.GetContainer(reference.container);
                var referenceResult = referenceResults[n];
                var ids = referenceResult.ids;
                if (ids.Count > 0) {
                    var refIdList   = ids.ToList();
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
                        referenceResult.references = await ReadReferences(subReferences, subEntities, syncResponse); 
                    }
                }
            }
            return referenceResults;
        }
    }


    public class EntityValue {
        public JsonValue    value;

        public EntityValue() { } // required for TypeMapper

        public EntityValue(string json) {
            value.json = json;
        }
    }
}
