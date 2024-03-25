// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Query entities from the given <see cref="container"/> using a <see cref="filter"/><br/>
    /// To return entities referenced by fields of the query result use <see cref="references"/>
    /// </summary>
    public sealed class QueryEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Serialize                            ("cont")]
        [Required]  public  ShortString         container;
                    public  SortOrder?          orderByKey;
        /// <summary>name of the primary key property of the returned entities</summary>
                    public  string              keyName;
                    public  bool?               isIntKey;
        /// <summary>
        /// query filter as JSON tree. <br/>
        /// Is used in favour of <see cref="filter"/> as its serialization is more performant
        /// </summary>
                    public  JsonValue           filterTree;
        /// <summary>
        /// query filter as a <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions">Lambda expression</a>
        /// returning a boolean value. E.g.<br/>
        /// <code>o => o.name == 'Smartphone'</code>
        /// if <see cref="filterTree"/> is assigned it has priority
        /// </summary>
                    public  string              filter;
        /// <summary>used to request the entities referenced by properties of the query task result</summary>
                    public  List<References>    references;
        /// <summary>limit the result set to the given number</summary>
                    public  int?                limit;
        /// <summary>If set the query uses or creates a <see cref="cursor"/> and return <see cref="maxCount"/> number of entities.</summary>
                    public  int?                maxCount;
        /// <summary>specify the <see cref="cursor"/> of a previous cursor request</summary>
                    public  string              cursor;
                        
        [Ignore]    private FilterOperation     filterLambda;
        [Ignore]    internal OperationContext   filterContext;
                        
        public   override   TaskType            TaskType => TaskType.query;
        public   override   string              TaskName => $"container: '{container}', filter: {filter}";
        
        public QueryEntities() { }
        
        public QueryEntities(
            ShortString         container,
            string              filter,
            JsonValue           filterTree,
            OperationContext    filterContext)
        {
            this.container      = container;
            this.filter         = filter;
            this.filterTree     = filterTree;
            this.filterContext  = filterContext;
            
        }
        
        public FilterOperation GetFilter() {
            if (filterLambda != null)
                return filterLambda;
            return Operation.FilterTrue;
        }
        
        internal static bool ValidateFilter(
            in  JsonValue           filterTree,
                string              filter,
                SyncContext         syncContext,
            ref FilterOperation     filterLambda,
            out TaskErrorResult     error)
        {
            error                   = null;
            if (!filterTree.IsNull()) {
                var pool                = syncContext.pool;
                var filterValidation    = syncContext.sharedCache.GetValidationType(typeof(FilterOperation));
                using (var pooled = pool.TypeValidator.Get()) {
                    var validator   = pooled.instance;
                    if (!validator.Validate(filterTree, filterValidation, out var validationError)) {
                        error = InvalidTaskError($"filterTree error: {validationError}");
                        return false;
                    }
                }
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader      = pooled.instance.reader;
                    var filterOp    = reader.Read<FilterOperation>(filterTree);
                    if (reader.Error.ErrSet) {
                        error = InvalidTaskError($"filterTree error: {reader.Error.msg.ToString()}");
                        return false;
                    }
                    filterLambda = filterOp;
                    return true;
                }
            }
            if (filter == null)
                return true;
            var operation = Operation.Parse(filter, out var parseError);
            if (operation == null) {
                error = InvalidTaskError(parseError);
                return false;
            }
            if (operation is FilterOperation filterOperation) {
                filterLambda = filterOperation;
                return true;
            }
            error = InvalidTaskError("filter must be boolean operation (a predicate)");
            return false;
        }
        
        private EntityContainer PrepareQuery(
            EntityDatabase          database,
            SyncContext             syncContext,
            out TaskErrorResult     error)
        {
            if (container.IsNull()) {
                error = MissingContainer();
                return null;
            }
            if (!ValidReferences(references, out error)) {
                return null;
            }
            if (!ValidateFilter (filterTree, filter, syncContext, ref filterLambda, out error)) {
                return null;
            }
            filterContext = new OperationContext();
            if (!filterContext.Init(GetFilter(), out var message)) {
                error = InvalidTaskError($"invalid filter: {message}");
                return null;
            }
            var entityContainer = database.GetOrCreateContainer(container);
            if (entityContainer == null) {
                error = ContainerNotFound();
                return null;
            }
            error = null;
            return entityContainer;
        }
        
        private void ProcessQueryResult(QueryEntitiesResult result)
        {
            var entities    = result.entities;
            var values      = entities.Values;
            if (orderByKey.HasValue) {
                Array.Sort(values, EntityValue.Comparer);
                if (orderByKey.Value == SortOrder.desc) {
                    Array.Reverse(values);
                }
            }
            result.container = container;
            if (values.Length > 0) {
                result.len = values.Length;
            }
        }
        
        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext)
        {
            var entityContainer = PrepareQuery(database, syncContext, out var error);
            if (error != null) {
                return error;
            }
            var result = await entityContainer.QueryEntitiesAsync(this, syncContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            ProcessQueryResult(result);
            if (references != null && references.Count > 0) {
                var read = await entityContainer.ReadReferencesAsync(references, result.entities, container, "", syncContext).ConfigureAwait(false);
                result.references   = read.references; 
            }
            return result;
        }
        
        public override SyncTaskResult Execute (EntityDatabase database, SyncResponse response, SyncContext syncContext)
        {
            var entityContainer = PrepareQuery(database, syncContext, out var error);
            if (error != null) {
                return error;
            }
            var result = entityContainer.QueryEntities(this, syncContext);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            ProcessQueryResult(result);
            if (references != null && references.Count > 0) {
                var read = entityContainer.ReadReferences(references, result.entities, container, "", syncContext);
                result.references   = read.references; 
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="QueryEntities"/> task
    /// </summary>
    public sealed class QueryEntitiesResult : SyncTaskResult, ITaskResultError
    {
        /// <summary>container name - not utilized by Protocol</summary>
        [Serialize                                ("cont")]
        [DebugInfo] public  ShortString             container;
                    public  string                  cursor;
        /// <summary>number of <see cref="set"/> values - not utilized by Protocol</summary>
        [DebugInfo] public  int?                    len;
        /// <summary>the result set of entities matching the given <see cref="QueryEntities.filter"/></summary>
        [Required]  public  ListOne<JsonValue>      set;
                    public  List<EntityError>       errors;
        /// <summary>the referenced entities specified in <see cref="QueryEntities.references"/></summary>
                    public  List<ReferencesResult>  references;
                    public  string                  sql;
                        
        [Ignore]    public  Entities                entities;
        [Ignore]    public  TaskExecuteError        Error { get; set; }

        
        internal override   TaskType                TaskType    => TaskType.query;
        internal override   bool                    Failed      => Error != null;
        public   override   string                  ToString()  => $"(container: {container})";
    }
    
    // ReSharper disable InconsistentNaming
    public enum SortOrder {
        asc     = 1,
        desc    = 2,
    }
}