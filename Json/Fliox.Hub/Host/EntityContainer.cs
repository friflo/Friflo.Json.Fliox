// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Tree;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// An <see cref="EntityContainer"/> is the abstraction of a collection / table used to store entities / records 
    /// as key value pairs.
    /// </summary>
    /// <remarks>
    /// It uses a string as key and a JSON object as value. Each container is intended to store the
    /// entities / records of a specific type. E.g. one container for storing JSON objects representing 'articles'
    /// another one for storing 'orders'.
    /// <para>
    ///   <see cref="EntityContainer"/> define the entire set of interfaces a database adapter needs to implement to
    ///   enable the complete feature set of <see cref="Client.EntitySet{TKey,T}"/> and <see cref="Client.FlioxClient"/>.
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
    ///         => An <see cref="EntityError"/> need to be added to task result errors.
    ///            E.g. add an error to <see cref="CreateEntitiesResult.errors"/> in case of
    ///            <see cref="FileContainer.CreateEntities"/>
    ///   </para>
    ///   
    ///   All ...Result types returned by the interface methods of <see cref="EntityContainer"/> like
    ///   <see cref="CreateEntities"/>, <see cref="ReadEntities"/>, ... implement <see cref="ICommandResult"/>.
    ///   In case a database command fails completely  <see cref="ICommandResult.Error"/> needs to be set.
    ///   See <see cref="FlioxHub.ExecuteSync"/> for proper error handling.
    /// </para>
    /// </remarks>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class EntityContainer : IDisposable
    {
    #region - members
        /// <summary> container name </summary>
        public    readonly  string                              name;
        /// <summary>
        /// The name used for a container / table instance in a specific database. By default it is equal to <see cref="name"/>.
        /// It can be customized (altered) by the <see cref="EntityDatabase.customContainerName"/> function.
        /// This field need to be used for <see cref="EntityContainer"/> implementations when accessing a specific
        /// databases (e.g. Mongo, Dynamo, Cosmos, Postgres, ...).
        /// </summary>
        protected readonly  string                              instanceName;
        internal  readonly  EntityDatabase                      database;
        internal  readonly  Dictionary<string, QueryEnumerator> cursors = new Dictionary<string, QueryEnumerator>();

        public    virtual   bool                                Pretty      => false;
        public    override  string                              ToString()  => $"{GetType().Name} - {instanceName}";
        #endregion

    #region - abstract container methods
        /// <summary>Create the entities specified in the given <paramref name="command"/></summary>
        public abstract Task<CreateEntitiesResult>    CreateEntities   (CreateEntities    command, SyncContext syncContext);
        /// <summary>Upsert the entities specified in the given <paramref name="command"/></summary>
        public abstract Task<UpsertEntitiesResult>    UpsertEntities   (UpsertEntities    command, SyncContext syncContext);
        /// <summary>Read entities by id with the ids passed in the given <paramref name="command"/></summary>
        public abstract Task<ReadEntitiesResult>      ReadEntities     (ReadEntities      command, SyncContext syncContext);
        /// <summary>Delete entities by id with the ids passed in the given <paramref name="command"/></summary>
        public abstract Task<DeleteEntitiesResult>    DeleteEntities   (DeleteEntities    command, SyncContext syncContext);
        /// <summary>Query entities using the filter in the given <paramref name="command"/></summary>
        public abstract Task<QueryEntitiesResult>     QueryEntities    (QueryEntities     command, SyncContext syncContext);
        /// <summary>Performs an aggregation specified in the given <paramref name="command"/></summary>
        public abstract Task<AggregateEntitiesResult> AggregateEntities(AggregateEntities command, SyncContext syncContext);
        #endregion

    #region - initialize
        protected EntityContainer(string name, EntityDatabase database) {
            this.name           = name;
            this.instanceName   = database.customContainerName(name);
            this.database       = database;
            database.AddContainer(this);
        }
        
        public virtual  void                        Dispose() { }
        
        #endregion

    #region - public utils
        /// <summary>Apply the given <paramref name="mergeEntities"/> to the container entities</summary>
        /// <remarks>
        /// Default implementation to apply patches to entities.
        /// The implementation perform three steps:
        /// 1. Read entities to be patches from a database
        /// 2. Apply merge patches
        /// 3. Write back the merged entities
        ///
        /// If the used database has integrated support for merging (patching) JSON its <see cref="EntityContainer"/>
        /// implementation can override this method to replace two database requests by one.
        /// </remarks>
        public virtual async Task<MergeEntitiesResult> MergeEntities (MergeEntities mergeEntities, SyncContext syncContext) {
            var patches = mergeEntities.patches;
            if (!EntityUtils.GetKeysFromEntities(mergeEntities.keyName, patches, syncContext, out string keyError)) {
                return new MergeEntitiesResult { Error = new CommandError(TaskErrorResultType.InvalidTask, keyError) };
            }
            var ids = new List<JsonKey>(patches.Count);
            foreach (var patch in patches) {
                ids.Add(patch.key);
            }
            // --- Read entities to be patched
            var readTask    = new ReadEntities { ids = ids, keyName = mergeEntities.keyName };
            var readResult  = await ReadEntities(readTask, syncContext).ConfigureAwait(false);
            
            if (readResult.Error != null) {
                return new MergeEntitiesResult { Error = readResult.Error };
            }
            var entities = readResult.entities;
            if (entities.Length != ids.Count)
                throw new InvalidOperationException($"MergeEntities: Expect entities.Count of response matches request. expect: {ids.Count} got: {entities.Length}");
            
            // --- Apply merges
            // iterate all patches and merge them to the entities read above
            var targets     = new  List<JsonEntity>  (entities.Length);
            var container   = mergeEntities.container;
            List<EntityError> patchErrors = null;
            using (var pooled = syncContext.pool.JsonMerger.Get())
            {
                JsonMerger merger   = pooled.instance;
                merger.Pretty       = Pretty;
                for (int n = 0; n < patches.Count; n++) {
                    var patch       = patches[n];
                    var key         = ids[n];
                    var entity      = entities[n];
                    var entityError = entity.Error; 
                    if (entityError != null) {
                        AddEntityError(ref patchErrors, key, entityError);
                        continue;
                    }
                    var target      = entity.Json;
                    if (target.IsNull()) {
                        var error = new EntityError(EntityErrorType.PatchError, container, key, "patch target not found");
                        AddEntityError(ref patchErrors, key, error);
                        continue;
                    }
                    // patch is an object - ensured by GetKeysFromEntities() above
                    var merge       = merger.Merge(target, patch.value);
                    var mergeError  = merger.Error;
                    if (mergeError != null) {
                        entityError = new EntityError(EntityErrorType.PatchError, container, key, mergeError);
                        AddEntityError(ref patchErrors, key, entityError);
                        continue;
                    }
                    targets.Add(new JsonEntity(key, merge));
                }
            }
            var valError = database.Schema?.ValidateEntities(container, targets, syncContext, EntityErrorType.PatchError, ref patchErrors);
            if (valError != null) {
                return new MergeEntitiesResult{Error = new CommandError(TaskErrorResultType.ValidationError, valError)};
            }
            
            // --- write merged entities back
            var task            = new UpsertEntities { entities = targets };
            var upsertResult    = await UpsertEntities(task, syncContext).ConfigureAwait(false);
            
            if (upsertResult.Error != null) {
                return new MergeEntitiesResult {Error = upsertResult.Error};
            }
            SyncResponse.AddEntityErrors(ref patchErrors, upsertResult.errors);
            return new MergeEntitiesResult{ errors = patchErrors };
        }
        
        /// Default implementation. Performs a full table scan! Act as reference and is okay for small data sets
        protected async Task<AggregateEntitiesResult> CountEntities (AggregateEntities command, SyncContext syncContext) {
            var query = new QueryEntities {
                container       = command.container,
                filter          = command.filter,
                filterTree      = command.filterTree,
                filterContext   = command.filterContext
            };
            var queryResult = await QueryEntities(query, syncContext).ConfigureAwait(false);
            
            var queryError = queryResult.Error; 
            if (queryError != null) {
                return new AggregateEntitiesResult { Error = queryError };
            }
            var value   = queryResult.entities.Length;
            var result  = new AggregateEntitiesResult { container = command.container, value = value };
            return result;
        }

        #endregion
    
    #region - internal methods
        internal string StoreCursor(QueryEnumerator enumerator, in JsonKey userId) {
            var cursor      = enumerator.Cursor;
            if (cursor != null) {
                cursors.Remove(cursor);
            }
            var nextCursor  = Guid.NewGuid().ToString();
            enumerator.Detach(nextCursor, this, userId);
            cursors.Add(nextCursor, enumerator);
            return nextCursor;
        }
        
        protected bool FindCursor(string cursor, SyncContext syncContext, out QueryEnumerator enumerator, out CommandError error) {
            if (cursor == null) {
                enumerator  = null;
                error       = null;
                return true;
            }
            var user = syncContext.User;
            if (user != null && cursors.TryGetValue(cursor, out enumerator)) {
                if (enumerator.UserId.IsEqual(user.userId)) {
                    enumerator.Attach();
                    error = null;
                    return true;
                }
            }
            enumerator  = null;
            error       = new CommandError(TaskErrorResultType.InvalidTask, $"cursor '{cursor}' not found");
            return false;
        }

        private static List<ReferencesResult> GetReferences(
            List<References>    references,
            EntityValue[]       entities,
            string              container,
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
                    ids         = new HashSet<JsonKey>(JsonKey.Equality)
                };
                referenceResults.Add(referenceResult);
            }
            var select      = new ScalarSelect(selectors);  // can be reused
            using (var pooled = syncContext.pool.ScalarSelector.Get()) {
                ScalarSelector selector = pooled.instance;
                // Get the selected refs for all entities.
                // Select() is expensive as it requires a full JSON parse. By using an selector array only one
                // parsing cycle is required. Otherwise for each selector Select() needs to be called individually.
                for (int i = 0; i < entities.Length; i++) {
                    var entity = entities[i];
                    if (entity.Error != null)
                        continue;
                    var         json    = entity.Json;
                    if (json.IsNull())
                        continue;
                    var selectorResults = selector.Select(json, select);
                    if (selectorResults == null) {
                        var error = new EntityError(EntityErrorType.ParseError, container, entity.key, selector.ErrorMessage);
                        // entity.SetError(entity.key, error); - used when using class EntityValue
                        entities[i] = new EntityValue(entity.key, error);
                        continue;
                    }
                    for (int n = 0; n < references.Count; n++) {
                        // selectorResults[n] contains Select() result of selectors[n] 
                        var entityRefs      = selectorResults[n].AsJsonKeys();
                        var referenceResult = referenceResults[n];
                        var ids             = referenceResult.ids;
                        ids.UnionWith(entityRefs);  // TAG_PERF (count & combine)
                        if (ids.Count > 0) {
                            referenceResult.count = ids.Count;     
                        }
                    }
                }
            }
            return referenceResults;
        }

        internal async Task<ReadReferencesResult> ReadReferences(
                List<References>    references,
                EntityValue[]       entities,
                string              container,
                string              selectorPath,
                SyncResponse        syncResponse,
                SyncContext         syncContext)
        {
            var referenceResults = GetReferences(references, entities, container, syncContext);
            
            // add referenced entities to ContainerEntities
            for (int n = 0; n < references.Count; n++) {
                var reference       = references[n];
                var refContName     = reference.container;
                var refCont         = database.GetOrCreateContainer(refContName);
                var referenceResult = referenceResults[n];
                var ids = referenceResult.ids;
                if (ids.Count == 0)
                    continue;
                var refIdList   = ids.ToList();
                var readRefIds  = new ReadEntities { ids = refIdList, keyName = reference.keyName, isIntKey = reference.isIntKey};
                var refEntities = await refCont.ReadEntities(readRefIds, syncContext).ConfigureAwait(false);
                
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
                var subEntities = new EntityValue [ids.Count];
                for (int i = 0; i < refEntities.entities.Length; i++) {
                    subEntities[i] = refEntities.entities[i];
                }
                var refReferencesResult =
                    await ReadReferences(subReferences, subEntities, refContName, subPath, syncResponse, syncContext).ConfigureAwait(false);
                // returned refReferencesResult.references is always set. Each references[] item contain either a result or an error.
                referenceResult.references = refReferencesResult.references;
            }
            return new ReadReferencesResult {references = referenceResults};
        }
        #endregion

    #region - public static utils
        protected static void AddEntityError(ref List<EntityError> errors, in JsonKey key, EntityError error) {
            if (errors == null) {
                errors = new List<EntityError>();
            }
            // add with TryAdd(). Only the first entity error is relevant. Subsequent entity errors are consequential failures.
            errors.Add(error);
        }
        #endregion
    }

    /// <see cref="ReadReferencesResult"/> is never serialized within a <see cref="SyncResponse"/> only its
    /// fields <see cref="references"/>.
    internal sealed class ReadReferencesResult
    {
        internal List<ReferencesResult> references;
    } 
}
