// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Query entities from the given <see cref="container"/> using a <see cref="filter"/><br/>
    /// To return entities referenced by fields of the query result use <see cref="references"/>
    /// </summary>
    public sealed class QueryEntities : SyncRequestTask
    {
        [Fri.Required]  public  string              container;
                        public  string              keyName;
                        public  bool?               isIntKey;
                        public  JsonValue           filterTree;
                        public  string              filter;
                        public  List<References>    references;
                        public  int?                limit;
                        public  int?                maxCount;
                        public  string              cursor;
                        
        [Fri.Ignore]    private FilterOperation     filterLambda;
        [Fri.Ignore]    public  OperationContext    filterContext;
                        
        internal override       TaskType            TaskType => TaskType.query;
        public   override       string              TaskName => $"container: '{container}', filter: {filter}";
        
        public FilterOperation GetFilter() {
            if (filterLambda != null)
                return filterLambda;
            return Operation.FilterTrue;
        }
        
        internal static bool ValidateFilter(
                JsonValue           filterTree,
                string              filter,
                ExecuteContext      executeContext,
            ref FilterOperation     filterLambda,
            out TaskErrorResult     error)
        {
            error                   = null;
            if (!filterTree.IsNull()) {
                var pool                = executeContext.pool;
                var filterValidation    = executeContext.sharedCache.GetValidationType(typeof(FilterOperation));
                using (var pooled = pool.TypeValidator.Get()) {
                    var validator   = pooled.instance;
                    if (!validator.ValidateField(filterTree, filterValidation, out var validationError)) {
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
                    filterLambda    = new Filter("o", filterOp);
                    return true;
                }
            }
            if (filter == null)
                return true;
            var operation = Operation.Parse("o=>" + filter, out var parseError);
            if (operation == null) {
                error = InvalidTaskError(parseError);
                return false;
            }
            if (operation is FilterOperation filterOperation) {
                filterLambda = filterOperation;
                return true;
            }
            error = InvalidTaskError("filter must be boolean operation");
            return false;
        }
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (container == null)
                return MissingContainer();
            if (!ValidReferences(references, out var error))
                return error;
            if (!ValidateFilter (filterTree, filter, executeContext, ref filterLambda, out error))
                return error;
            filterContext = new OperationContext();
            if (!filterContext.Init(GetFilter(), out var message)) {
                return InvalidTaskError($"invalid filter: {message}");
            }
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.QueryEntities(this, executeContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            var containerResult = response.GetContainerResult(container);
            var entities = result.entities;
            result.entities = null;  // clear -> its not part of protocol
            containerResult.AddEntities(entities);
            var queryRefsResults = new ReadReferencesResult();
            if (references != null && references.Count > 0) {
                queryRefsResults =
                    await entityContainer.ReadReferences(references, entities, container, "", response, executeContext).ConfigureAwait(false);
                // returned queryRefsResults.references is always set. Each references[] item contain either a result or an error.
            }
            result.container    = container;
            var ids             = entities.Keys.ToHashSet(JsonKey.Equality); // TAG_PERF
            result.ids          = ids;
            if (ids.Count > 0) {
                result.count        = ids.Count;
            }
            result.references   = queryRefsResults.references;
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class QueryEntitiesResult : SyncTaskResult, ICommandResult
    {
        [DebugInfo]     public  string                          container;
                        public  string                          cursor;
        /// <summary>number of <see cref="ids"/> - not utilized by Protocol</summary>
        [DebugInfo]     public  int?                            count;
        [Fri.Required]  public  HashSet<JsonKey>                ids = new HashSet<JsonKey>(JsonKey.Equality);
                        public  List<ReferencesResult>          references;
                        
        [Fri.Ignore]    public  Dictionary<JsonKey,EntityValue> entities;
        [Fri.Ignore]    public  CommandError                    Error { get; set; }

        
        internal override       TaskType                        TaskType => TaskType.query;
        public   override       string                          ToString() => $"(container: {container})";
    }
}