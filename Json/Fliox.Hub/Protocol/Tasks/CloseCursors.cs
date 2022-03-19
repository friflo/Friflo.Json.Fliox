// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Close the <see cref="cursors"/> of the given <see cref="container"/>
    /// </summary>
    public sealed class CloseCursors : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Fri.Required]  public  string          container;
        /// <summary>list of <see cref="cursors"/></summary>
                        public  List<string>    cursors;
        internal override       TaskType        TaskType => TaskType.closeCursors;
        
        public   override       string          TaskName => $"container: '{container}', cursor: {cursors}";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (container == null)
                return Task.FromResult<SyncTaskResult>(MissingContainer());
            
            var entityContainer     = database.GetOrCreateContainer(container);
            RemoveCursors(entityContainer, cursors, executeContext.User);
            
            var count               = entityContainer.cursors.Count;
            SyncTaskResult result   = new CloseCursorsResult { count = count };
            return Task.FromResult(result);
        }
        
        ///<summary> Note: A <paramref name="user"/> can remove only its own cursors </summary>
        private static void RemoveCursors(EntityContainer entityContainer, List<string> cursors, User user) {
            var containerCursors = entityContainer.cursors;
            if (cursors == null) {
                foreach (var pair in containerCursors) {
                    var containerCursor = pair.Value;
                    if (!containerCursor.UserId.IsEqual(user.userId))
                        continue;
                    containerCursor.Attach();
                    containerCursor.Dispose();
                }
                return;
            }
            foreach (var cursor in cursors) {
                if (!containerCursors.TryGetValue(cursor, out var containerCursor))
                    continue;
                if (!containerCursor.UserId.IsEqual(user.userId))
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