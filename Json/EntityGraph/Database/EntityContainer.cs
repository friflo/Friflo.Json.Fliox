// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph.Database
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

        public async Task<List<ReadReferenceResult>> ReadReferences(
                List<ReadReference>             references,
                Dictionary<string, EntityValue> entities,
                SyncResponse                    syncResponse)
        {
            var jsonPath    = SyncContext.scalarSelector;
            var referenceResults = new List<ReadReferenceResult>();
            foreach (var reference in references) {
                var refContainer = database.GetContainer(reference.container);
                var referenceResult = new ReadReferenceResult {
                    container   = reference.container,
                    ids         = new List<string>()
                };
                foreach (var id in reference.ids) {
                    EntityValue refEntity = entities[id];
                    if (refEntity == null) {
                        throw new InvalidOperationException($"expect entity reference available: {id}");
                    }
                    // todo call Select() only once with multiple selectors 
                    var select = new ScalarSelect(reference.refPath);
                    var selectorResults = jsonPath.Select(refEntity.value.json, select);
                    var refIds = selectorResults[0].AsStrings();
                    referenceResult.ids.AddRange(refIds);
                    
                    // add references to ContainerEntities
                    var readRefIds = new ReadEntities {ids = refIds};
                    var refEntities = await refContainer.ReadEntities(readRefIds);
                    var containerResult = syncResponse.GetContainerResult(reference.container);
                    containerResult.AddEntities(refEntities.entities);
                }
                referenceResults.Add(referenceResult);
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
