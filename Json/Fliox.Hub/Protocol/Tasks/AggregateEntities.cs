// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class AggregateEntities : SyncRequestTask
    {
        [Fri.Required]  public  string              container;
                    //  public  string              keyName;
                    //  public  bool?               isIntKey;
                        public  FilterOperation     filterTree;
                        public  string              filter;
                    //  public  List<References>    references;
                        
        [Fri.Ignore]    private FilterOperation     filterLambda;
        [Fri.Ignore]    public  OperationContext    filterContext;
                        
        internal override       TaskType            TaskType => TaskType.aggregate;
        public   override       string              TaskName => $"container: '{container}', filter: {filter}";
        
        public FilterOperation GetFilter() {
            if (filterLambda != null)
                return filterLambda;
            return Operation.FilterTrue;
        }

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            //  if (!ValidReferences(references, out var error))
            //      return error;
            if (!QueryEntities.ValidateFilter (filterTree, filter, ref filterLambda, out var error))
                return error;
            filterContext = new OperationContext();
            if (!filterContext.Init(GetFilter(), out var message)) {
                return InvalidTaskError($"invalid filter: {message}");
            }
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.AggregateEntities(this, messageContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            /*
            var containerResult = response.GetContainerResult(container);
            var entities = result.entities;
            result.entities = null;  // clear -> its not part of protocol
            containerResult.AddEntities(entities);
            var queryRefsResults = new ReadReferencesResult();
            if (references != null && references.Count > 0) {
                queryRefsResults =
                    await entityContainer.ReadReferences(references, entities, container, "", response, messageContext).ConfigureAwait(false);
                // returned queryRefsResults.references is always set. Each references[] item contain either a result or an error.
            }
            result.container    = container;
            result.ids          = entities.Keys.ToHashSet(JsonKey.Equality); // TAG_PERF
            result.references   = queryRefsResults.references;
            */
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class AggregateEntitiesResult : SyncTaskResult, ICommandResult
    {
                        public  string                      container;  // only for debugging ergonomics
                        public  Dictionary<string, long>    counts = new Dictionary<string, long>();
        [Fri.Ignore]    public  CommandError                Error { get; set; }

        
        internal override   TaskType            TaskType => TaskType.aggregate;
        public   override   string              ToString() => $"(container: {container})";
    }
}