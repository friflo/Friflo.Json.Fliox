// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Tree;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    internal static class EntityContainerUtils
    {
        internal static List<JsonEntity> ApplyMerges(
                EntityContainer     entityContainer,
                MergeEntities       mergeEntities,
                ReadEntitiesResult  readResult,
                ListOne<JsonKey>    ids,
                SharedEnv           env,
            ref List<EntityError>   patchErrors,
            out TaskExecuteError    taskError)
        {
            if (readResult.Error != null) {
                taskError = readResult.Error; 
                return null;
            }
            var values = readResult.entities.Values;
            if (values.Length != ids.Count) {
                throw new InvalidOperationException($"MergeEntities: Expect entities.Count of response matches request. expect: {ids.Count} got: {values.Length}");
            }
            
            // --- Apply merges
            // iterate all patches and merge them to the entities read above
            var targets     = new List<JsonEntity>  (values.Length);
            var container   = mergeEntities.container;
            var patches     = mergeEntities.patches;
            
            using (var pooled = env.pool.JsonMerger.Get())
            {
                JsonMerger merger   = pooled.instance;
                merger.Pretty       = entityContainer.Pretty;
                for (int n = 0; n < patches.Count; n++) {
                    var patch       = patches[n];
                    var key         = ids[n];
                    var entity      = values[n];
                    var entityError = entity.Error; 
                    if (entityError != null) {
                        EntityContainer.AddEntityError(ref patchErrors, key, entityError);
                        continue;
                    }
                    var target      = entity.Json;
                    if (target.IsNull()) {
                        var error = new EntityError(EntityErrorType.PatchError, container, key, "patch target not found");
                        EntityContainer.AddEntityError(ref patchErrors, key, error);
                        continue;
                    }
                    // patch is an object - ensured by GetKeysFromEntities() above
                    var merge       = merger.Merge(target, patch.value);
                    var mergeError  = merger.Error;
                    if (mergeError != null) {
                        entityError = new EntityError(EntityErrorType.PatchError, container, key, mergeError);
                        EntityContainer.AddEntityError(ref patchErrors, key, entityError);
                        continue;
                    }
                    targets.Add(new JsonEntity(key, merge));
                }
            }
            var schema      = entityContainer.database.Schema;
            var valError    = schema?.ValidateEntities(container, targets, env, EntityErrorType.PatchError, ref patchErrors);
            if (valError != null) {
                taskError = new TaskExecuteError(TaskErrorType.ValidationError, valError);
                return null;
            }
            taskError = null;
            return targets;
        }
        
        /// <summary>
        /// Return the ids - foreign keys - stored in the <see cref="References.selector"/> fields
        /// of the given <paramref name="entities"/>.
        /// </summary>
        internal static List<ReferencesResult> GetReferences(
            List<References>    references,
            in Entities         entities,
            in ShortString      container,
            SyncContext         syncContext)
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
                    foreignKeys = new ListOne<JsonKey>(),
                    set         = new ListOne<JsonValue>()
                };
                referenceResults.Add(referenceResult);
            }
            var select  = new ScalarSelect(selectors);  // can be reused
            var values  = entities.Values;
            using (var pooled = syncContext.pool.ScalarSelector.Get()) {
                ScalarSelector selector = pooled.instance;
                // Get the selected refs for all entities.
                // Select() is expensive as it requires a full JSON parse. By using an selector array only one
                // parsing cycle is required. Otherwise for each selector Select() needs to be called individually.
                for (int i = 0; i < values.Length; i++) {
                    var entity = values[i];
                    if (entity.Error != null)
                        continue;
                    var json = entity.Json;
                    if (json.IsNull())
                        continue;
                    var selectorResults = selector.Select(json, select);
                    if (selectorResults == null) {
                        var error = new EntityError(EntityErrorType.ParseError, container, entity.key, selector.ErrorMessage);
                        // entity.SetError(entity.key, error); - used when using class EntityValue
                        values[i] = new EntityValue(entity.key, error);
                        continue;
                    }
                    for (int n = 0; n < references.Count; n++) {
                        // selectorResults[n] contains Select() result of selectors[n] 
                        var referenceResult = referenceResults[n];
                        var selectorResult  = selectorResults[n];
                        selectorResult.AddKeysToList(referenceResult.foreignKeys);
                    }
                }
            }
            // --- remove duplicate ids 
            var idSet = Helper.CreateHashSet(0, JsonKey.Equality);
            for (int n = 0; n < referenceResults.Count; n++)
            {
                var referenceResult = referenceResults[n];
                var foreignKeys = referenceResult.foreignKeys;
                idSet.Clear();
                idSet.UnionWith(foreignKeys); // deduplicate
                foreignKeys.Clear();
                foreach (var id in idSet) {
                    foreignKeys.Add(id);
                }
                KeyValueUtils.OrderKeys(foreignKeys, references[n].orderByKey);
            }
            return referenceResults;
        }
        
        internal static bool ProcessRefEntities(
                References          reference,
                ReferencesResult    referenceResult,
                in ShortString      container,
                string              selectorPath,
                ReadEntitiesResult  refEntities,
            out Entities            subEntities,
            out string              subPath)
        {
            subPath = $"{selectorPath} -> {reference.selector}";
            // In case of ReadEntities error: Assign error to result and continue with other references.
            // Resolving other references are independent may be successful.
            if (refEntities.Error != null) {
                var message = $"read references failed: '{container.AsString()}{subPath}' - {refEntities.Error.message}";
                referenceResult.error       = message;
                referenceResult.entities    = new Entities(Array.Empty<EntityValue>());
                subPath = null;
                subEntities = default;
                return false;
            }
            referenceResult.entities = refEntities.entities;
                
            var subReferences = reference.references;
            if (subReferences == null) {
                subPath = null;
                subEntities = default;
                return false;
            }
            var subEntitiesArray    = new EntityValue [referenceResult.foreignKeys.Count];
            subEntities             = new Entities(subEntitiesArray);
            var refValues           = refEntities.entities.Values;
            for (int i = 0; i < refValues.Length; i++) {
                subEntitiesArray[i] = refValues[i];
            }
            return true;
        }
    }


    /// <see cref="ReadReferencesResult"/> is never serialized within a <see cref="SyncResponse"/> only its
    /// fields <see cref="references"/>.
    internal sealed class ReadReferencesResult
    {
        internal List<ReferencesResult> references;
    } 
}
