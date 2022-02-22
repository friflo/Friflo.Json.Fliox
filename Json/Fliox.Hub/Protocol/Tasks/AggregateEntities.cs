// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class AggregateEntities : SyncRequestTask
    {
        [Fri.Required]  public      string              container;
                        public      AggregateType       type;
                        public      JsonValue           filterTree;
                        public      string              filter;
                        
        [Fri.Ignore]    private     FilterOperation     filterLambda;
        [Fri.Ignore]    public      OperationContext    filterContext;
                        
        internal override           TaskType            TaskType => TaskType.aggregate;
        public   override           string              TaskName => $"container: '{container}',type: {type}, filter: {filter}";
        
        public FilterOperation GetFilter() {
            if (filterLambda != null)
                return filterLambda;
            return Operation.FilterTrue;
        }

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (!QueryEntities.ValidateFilter (filterTree, filter, messageContext, ref filterLambda, out var error))
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
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class AggregateEntitiesResult : SyncTaskResult, ICommandResult
    {
        [DebugInfo]     public  string          container;
                        public  double?         value;      // set if not using groupBy
        [Fri.Ignore]    public  CommandError    Error { get; set; }

        
        internal override   TaskType            TaskType => TaskType.aggregate;
        public   override   string              ToString() => $"(container: {container})";
    }
    
    public enum AggregateType {
        count
    }
}