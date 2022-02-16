// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class CloseCursor : SyncRequestTask
    {
        [Fri.Required]  public  string          container;
        [Fri.Required]  public  string          cursor;
        internal override       TaskType        TaskType => TaskType.closeCursor;
        
        public   override       string          TaskName => $"container: '{container}', cursor: {cursor}";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return Task.FromResult<SyncTaskResult>(MissingContainer());
            if (cursor == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(cursor)));
            
            var entityContainer = database.GetOrCreateContainer(container);
            entityContainer.cursors.Remove(cursor);
            SyncTaskResult result = new CloseCursorResult();
            return Task.FromResult(result);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class CloseCursorResult : SyncTaskResult
    {
        internal override   TaskType        TaskType => TaskType.closeCursor;
    }
}