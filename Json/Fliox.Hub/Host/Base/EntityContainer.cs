// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Tree;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
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
    ///         => <see cref="ITaskResultError.Error"/> need to be set.
    ///   </para> 
    ///   <para>2. The database request was successful, but one or more entities (key/values) had an error when accessing.
    ///         E.g. Writing an entity to a file with a <see cref="FileContainer"/> fails because it is used by another process.
    ///         => An <see cref="EntityError"/> need to be added to task result errors.
    ///            E.g. add an error to <see cref="CreateEntitiesResult.errors"/> in case of
    ///            <see cref="FileContainer.CreateEntitiesAsync"/>
    ///   </para>
    ///   
    ///   All ...Result types returned by the interface methods of <see cref="EntityContainer"/> like
    ///   <see cref="CreateEntitiesAsync"/>, <see cref="ReadEntitiesAsync"/>, ... implement <see cref="ITaskResultError"/>.
    ///   In case a database command fails completely  <see cref="ITaskResultError.Error"/> needs to be set.
    ///   See <see cref="FlioxHub.ExecuteRequestAsync"/> for proper error handling.
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
        [DebuggerBrowsable(Never)]
        public    readonly  ShortString                         nameShort;
        /// <summary>
        /// The name used for a container / table instance in a specific database. By default it is equal to <see cref="name"/>.
        /// It can be customized (altered) by the <see cref="EntityDatabase.CustomContainerName"/> function.
        /// This field need to be used for <see cref="EntityContainer"/> implementations when accessing a specific
        /// databases (e.g. Mongo, Dynamo, Cosmos, Postgres, ...).
        /// </summary>
        protected readonly  string                              instanceName;
        internal  readonly  EntityDatabase                      database;
        internal  readonly  Dictionary<string, QueryEnumerator> cursors = new Dictionary<string, QueryEnumerator>();
        internal  readonly  string                              keyName;

        public    virtual   bool                                Pretty      => false;
        public    override  string                              ToString()  => $"{GetType().Name} - {instanceName}";
        #endregion

    #region - abstract container methods
        /// <summary>Create the entities specified in the given <paramref name="command"/></summary>
        public abstract Task<CreateEntitiesResult>    CreateEntitiesAsync   (CreateEntities    command, SyncContext syncContext);
        /// <summary>Upsert the entities specified in the given <paramref name="command"/></summary>
        public abstract Task<UpsertEntitiesResult>    UpsertEntitiesAsync   (UpsertEntities    command, SyncContext syncContext);
        /// <summary>Read entities by id with the ids passed in the given <paramref name="command"/></summary>
        public abstract Task<ReadEntitiesResult>      ReadEntitiesAsync     (ReadEntities      command, SyncContext syncContext);
        /// <summary>Delete entities by id with the ids passed in the given <paramref name="command"/></summary>
        public abstract Task<DeleteEntitiesResult>    DeleteEntitiesAsync   (DeleteEntities    command, SyncContext syncContext);
        /// <summary>Query entities using the filter in the given <paramref name="command"/></summary>
        public abstract Task<QueryEntitiesResult>     QueryEntitiesAsync    (QueryEntities     command, SyncContext syncContext);
        /// <summary>Performs an aggregation specified in the given <paramref name="command"/></summary>
        public abstract Task<AggregateEntitiesResult> AggregateEntitiesAsync(AggregateEntities command, SyncContext syncContext);
        
        // ------------------------------- synchronous version -------------------------------
        public virtual CreateEntitiesResult    CreateEntities   (CreateEntities    command, SyncContext syncContext) => throw new NotImplementedException();
        /// <summary>Upsert the entities specified in the given <paramref name="command"/></summary>
        public virtual UpsertEntitiesResult    UpsertEntities   (UpsertEntities    command, SyncContext syncContext) => throw new NotImplementedException();
        /// <summary>Read entities by id with the ids passed in the given <paramref name="command"/></summary>
        public virtual ReadEntitiesResult      ReadEntities     (ReadEntities      command, SyncContext syncContext) => throw new NotImplementedException();
        /// <summary>Delete entities by id with the ids passed in the given <paramref name="command"/></summary>
        public virtual DeleteEntitiesResult    DeleteEntities   (DeleteEntities    command, SyncContext syncContext) => throw new NotImplementedException();
        /// <summary>Query entities using the filter in the given <paramref name="command"/></summary>
        public virtual QueryEntitiesResult     QueryEntities    (QueryEntities     command, SyncContext syncContext) => throw new NotImplementedException();
        
        // not supported:  virtual AggregateEntitiesResult AggregateEntitiesSync(AggregateEntities command, SyncContext syncContext)
        
        #endregion

    #region - initialize
        protected EntityContainer(string name, EntityDatabase database) {
            this.name           = name;
            this.nameShort      = new ShortString(name);
            this.instanceName   = database.CustomContainerName(name);
            this.database       = database;
            var typeSchema      = database.Schema?.typeSchema;
            if (typeSchema != null) {
                keyName = typeSchema.RootType.FindField(name)?.type.KeyField.name;
            }
            database.AddContainer(this);
        }
        
        public virtual  void                        Dispose() { }
        
        #endregion

    #region - public utils
        /// <summary>Can be implemented used to merge entities synchronously for optimization</summary>
        public virtual MergeEntitiesResult MergeEntities (MergeEntities mergeEntities, SyncContext syncContext)
            => throw new NotSupportedException();
        
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
        public virtual async Task<MergeEntitiesResult> MergeEntitiesAsync (MergeEntities mergeEntities, SyncContext syncContext) {
            var patches = mergeEntities.patches;
            var env     = syncContext.sharedEnv;
            var ids     = new List<JsonKey>(patches.Count);
            foreach (var patch in patches) {
                ids.Add(patch.key);
            }
            // --- Read entities to be patched
            var readTask    = new ReadEntities { ids = ids, keyName = mergeEntities.keyName };
            var readResult  = await ReadEntitiesAsync(readTask, syncContext).ConfigureAwait(false);
            
            if (readResult.Error != null) {
                return new MergeEntitiesResult { Error = readResult.Error };
            }
            var values = readResult.entities.values;
            if (values.Length != ids.Count)
                throw new InvalidOperationException($"MergeEntities: Expect entities.Count of response matches request. expect: {ids.Count} got: {values.Length}");
            
            // --- Apply merges
            // iterate all patches and merge them to the entities read above
            var targets     = new  List<JsonEntity>  (values.Length);
            var container   = mergeEntities.container;
            List<EntityError> patchErrors = null;
            using (var pooled = env.pool.JsonMerger.Get())
            {
                JsonMerger merger   = pooled.instance;
                merger.Pretty       = Pretty;
                for (int n = 0; n < patches.Count; n++) {
                    var patch       = patches[n];
                    var key         = ids[n];
                    var entity      = values[n];
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
            var valError = database.Schema?.ValidateEntities(container, targets, env, EntityErrorType.PatchError, ref patchErrors);
            if (valError != null) {
                return new MergeEntitiesResult{Error = new TaskExecuteError(TaskErrorType.ValidationError, valError)};
            }
            
            // --- write merged entities back
            var task            = new UpsertEntities { entities = targets };
            var upsertResult    = await UpsertEntitiesAsync(task, syncContext).ConfigureAwait(false);
            
            if (upsertResult.Error != null) {
                return new MergeEntitiesResult {Error = upsertResult.Error};
            }
            SyncResponse.AddEntityErrors(ref patchErrors, upsertResult.errors);
            return new MergeEntitiesResult{ errors = patchErrors };
        }
        
        /// Default implementation. Performs a full table scan! Act as reference and is okay for small data sets
        protected async Task<AggregateEntitiesResult> CountEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var query = new QueryEntities {
                container       = command.container,
                filter          = command.filter,
                filterTree      = command.filterTree,
                filterContext   = command.filterContext
            };
            var queryResult = await QueryEntitiesAsync(query, syncContext).ConfigureAwait(false);
            
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
        /// <summary>
        /// Create and return a unique cursor id and store the given <paramref name="enumerator"/> with this id.<br/>
        /// The cursor id created by a previous call for the given <paramref name="enumerator"/> will be removed.
        /// </summary>
        protected string StoreCursor(QueryEnumerator enumerator, User user) {
            if (enumerator == null) throw new ArgumentNullException(nameof(enumerator));
            var cursor      = enumerator.Cursor;
            if (cursor != null) {
                cursors.Remove(cursor);
            }
            var nextCursor  = Guid.NewGuid().ToString();
            var userId      = user?.userId ?? default;
            enumerator.Detach(nextCursor, this, userId);
            cursors.Add(nextCursor, enumerator);
            return nextCursor;
        }
        
        /// <summary>
        /// Remove the given <paramref name="enumerator"/> from stored cursors.<br/>
        /// The given <paramref name="enumerator"/> can be null.
        /// </summary>
        protected void RemoveCursor(QueryEnumerator enumerator) {
            if (enumerator == null) {
                return;
            }
            var cursor      = enumerator.Cursor;
            if (cursor != null) {
                cursors.Remove(cursor);
            }
        }
        
        /// <summary>
        /// Find the <paramref name="enumerator"/> for the given <paramref name="cursor"/> id.<br/>
        /// Return true if the <paramref name="cursor"/> was found. Otherwise false.
        /// </summary>
        protected bool FindCursor(string cursor, SyncContext syncContext, out QueryEnumerator enumerator, out TaskExecuteError error) {
            if (cursor == null) {
                enumerator  = null;
                error       = null;
                return true;
            }
            if (cursors.TryGetValue(cursor, out enumerator)) {
                var userId = syncContext.User?.userId ?? default;
                if (enumerator.UserId.IsEqual(userId)) {
                    enumerator.Attach();
                    error = null;
                    return true;
                }
            }
            enumerator  = null;
            error       = new TaskExecuteError(TaskErrorType.InvalidTask, $"cursor '{cursor}' not found");
            return false;
        }

        private static List<ReferencesResult> GetReferences(
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
                    ids         = new List<JsonKey>()
                };
                referenceResults.Add(referenceResult);
            }
            var select  = new ScalarSelect(selectors);  // can be reused
            var values  = entities.values;
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
                        var entityRefs      = selectorResults[n].AsJsonKeys();
                        var referenceResult = referenceResults[n];
                        var ids             = referenceResult.ids;
                        var set             = Helper.CreateHashSet(ids.Count, JsonKey.Equality);
                        foreach (var id in ids) {
                            set.Add(id);
                        }
                        set.UnionWith(entityRefs);
                        ids.Clear();
                        foreach (var id in set) {
                            ids.Add(id);
                        }
                        KeyValueUtils.OrderKeys(ids, references[n].orderByKey);
                        if (ids.Count > 0) {
                            referenceResult.len = ids.Count;     
                        }
                    }
                }
            }
            return referenceResults;
        }

        internal async Task<ReadReferencesResult> ReadReferencesAsync(
                List<References>    references,
                Entities            entities,
                ShortString         container,
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
                var refEntities = await refCont.ReadEntitiesAsync(readRefIds, syncContext).ConfigureAwait(false);
                
                var subPath = $"{selectorPath} -> {reference.selector}";
                // In case of ReadEntities error: Assign error to result and continue with other references.
                // Resolving other references are independent may be successful.
                if (refEntities.Error != null) {
                    var message = $"read references failed: '{container.AsString()}{subPath}' - {refEntities.Error.message}";
                    referenceResult.error = message;
                    continue;
                }
                var containerResult = syncResponse.GetContainerResult(refContName);
                containerResult.AddEntities(refEntities.entities);
                var subReferences = reference.references;  
                
                if (subReferences == null)
                    continue;
                var subEntitiesArray    = new EntityValue [ids.Count];
                var subEntities         = new Entities(subEntitiesArray);
                var refValues           = refEntities.entities.values;
                for (int i = 0; i < refValues.Length; i++) {
                    subEntitiesArray[i] = refValues[i];
                }
                var refReferencesResult =
                    await ReadReferencesAsync(subReferences, subEntities, refContName, subPath, syncResponse, syncContext).ConfigureAwait(false);
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
        
        public static TaskExecuteError NotImplemented (string message) {
            return new TaskExecuteError(TaskErrorType.NotImplemented, message);
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
