// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class CloseCursors : SyncRequestTask
    {
        [Fri.Required]  public  string          container;
                        public  List<string>    cursors;
        internal override       TaskType        TaskType => TaskType.closeCursors;
        
        public   override       string          TaskName => $"container: '{container}', cursor: {cursors}";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return Task.FromResult<SyncTaskResult>(MissingContainer());
            
            var entityContainer     = database.GetOrCreateContainer(container);
            RemoveCursors(entityContainer, cursors);
            
            var count               = entityContainer.cursors.Count;
            SyncTaskResult result   = new CloseCursorsResult { count = count };
            return Task.FromResult(result);
        }
        
        private static void RemoveCursors(EntityContainer entityContainer, List<string> cursors) {
            var containerCursors = entityContainer.cursors;
            if (cursors == null) {
                foreach (var pair in containerCursors) {
                    var containerCursor = pair.Value;
                    containerCursor.Attach();
                    containerCursor.Dispose();
                }
                return;
            }
            foreach (var cursor in cursors) {
                if (!containerCursors.TryGetValue(cursor, out var containerCursor))
                    continue;
                containerCursor.Attach();
                containerCursor.Dispose();
            }
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class CloseCursorsResult : SyncTaskResult
    {
        public              int             count;
        internal override   TaskType        TaskType => TaskType.closeCursors;
    }
}