// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;
using static Friflo.Json.Fliox.Hub.Host.EntityContainerUtils;

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
    public abstract partial class EntityContainer : IDisposable
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
        public    readonly  string                              keyName;

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
        /// <summary>Performs an aggregation specified in the given <paramref name="command"/></summary>
        public virtual AggregateEntitiesResult AggregateEntities(AggregateEntities command, SyncContext syncContext) => throw new NotImplementedException();
        
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

    #region - cursor methods
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
        /// Find the enumerator for the given <paramref name="cursor"/> id.<br/>
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
        #endregion
        
    #region - sync / async
        // ----------------------------------------- sync / async -----------------------------------------
        
        /// <summary>Apply the given <paramref name="mergeEntities"/> to the container entities</summary>
        /// <remarks>
        /// Default implementation to apply patches to entities.
        /// The implementation perform three steps:
        /// 1. Read entities to be patches from a database
        /// 2. Apply merge patches
        /// 3. Write back the merged entities
        ///
        /// If the used database has integrated support for merging (patching) JSON its <see cref="EntityContainer"/>
        /// implementation can override this method to replace two database requests by one.<br/>
        /// <br/>
        /// Counterpart of <see cref="MergeEntities"/>
        /// </remarks>
        public virtual async Task<MergeEntitiesResult> MergeEntitiesAsync (MergeEntities mergeEntities, SyncContext syncContext) {
            var patches = mergeEntities.patches;
            var ids     = new ListOne<JsonKey>(patches.Count);
            foreach (var patch in patches) {
                ids.Add(patch.key);
            }
            // --- Read entities to be patched
            var readTask    = new ReadEntities { ids = ids, keyName = mergeEntities.keyName };
            var readResult  = await ReadEntitiesAsync(readTask, syncContext).ConfigureAwait(false);
            
            List<EntityError> patchErrors = null;
            var targets = ApplyMerges(this, mergeEntities, readResult, ids, syncContext.sharedEnv, ref patchErrors, out var error);
            if (error != null) {
                return new MergeEntitiesResult { Error = error };   
            }
            // --- write merged entities back
            var task            = new UpsertEntities { entities = targets };
            var upsertResult    = await UpsertEntitiesAsync(task, syncContext).ConfigureAwait(false);
            
            if (upsertResult.Error != null) {
                return new MergeEntitiesResult { Error = upsertResult.Error };
            }
            SyncResponse.AddEntityErrors(ref patchErrors, upsertResult.errors);
            return new MergeEntitiesResult{ errors = patchErrors };
        }
        
        /// <summary>
        /// Default implementation. Performs a full table scan! Act as reference and is okay for small data sets.<br/>
        /// Counterpart <see cref="CountEntities"/>
        /// </summary>
        protected async Task<AggregateEntitiesResult> CountEntitiesAsync (AggregateEntities command, SyncContext syncContext)
        {
            var query = new QueryEntities (command.container, command.filter, command.filterTree, command.filterContext);
            var queryResult = await QueryEntitiesAsync(query, syncContext).ConfigureAwait(false);
            
            var queryError = queryResult.Error; 
            if (queryError != null) {
                return new AggregateEntitiesResult { Error = queryError };
            }
            return new AggregateEntitiesResult { container = command.container, value = queryResult.entities.Length };
        }

        /// <summary>
        /// Return the <see cref="ReferencesResult.entities"/> referenced by the <see cref="References.selector"/> path
        /// of the given <paramref name="entities"/>.<br/>
        ///
        /// Counterpart <see cref="ReadReferences"/>
        /// </summary>
        internal async Task<ReadReferencesResult> ReadReferencesAsync(
                List<References>    references,
                Entities            entities,
                ShortString         container,
                string              selectorPath,
                SyncContext         syncContext)
        {
            var referenceResults = GetReferences(references, entities, container, syncContext);
            
            for (int n = 0; n < references.Count; n++) {
                var reference       = references[n];
                var refCont         = database.GetOrCreateContainer(reference.container);
                var referenceResult = referenceResults[n];
                var foreignKeys     = referenceResult.foreignKeys;
                if (foreignKeys.Count == 0) {
                    referenceResult.entities = new Entities(Array.Empty<EntityValue>());
                    continue;
                }
                var readRefIds  = new ReadEntities { ids = foreignKeys, keyName = reference.keyName, isIntKey = reference.isIntKey};
                
                // read all referenced entities with a single read command.
                var refEntities = await refCont.ReadEntitiesAsync(readRefIds, syncContext).ConfigureAwait(false);
                
                if (!ProcessRefEntities(reference, referenceResult, container, selectorPath, refEntities, out var subEntities, out var subPath)) {
                    continue;
                }
                var refResult = await ReadReferencesAsync(reference.references, subEntities, reference.container, subPath, syncContext).ConfigureAwait(false);
                // returned refResult.references is always set. Each references[] item contain either a result or an error.
                referenceResult.references = refResult.references;
            }
            return new ReadReferencesResult { references = referenceResults };
        }
        // --------------------------------------- end: sync / async ---------------------------------------
        #endregion

    #region - public static utils
        internal static void AddEntityError(ref List<EntityError> errors, in JsonKey key, EntityError error) {
            if (errors == null) {
                errors = new List<EntityError>();
            }
            // add with TryAdd(). Only the first entity error is relevant. Subsequent entity errors are consequential failures.
            errors.Add(error);
        }
        
        protected static TaskExecuteError NotImplemented (string message) {
            return new TaskExecuteError(TaskErrorType.NotImplemented, message);
        }
        #endregion
    }
}
