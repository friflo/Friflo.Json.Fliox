// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Close the <see cref="cursors"/> of the given <see cref="container"/>
    /// </summary>
    public sealed class CloseCursors : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Serialize                        ("cont")]
        [Required]  public  ShortString     container;
        /// <summary>list of <see cref="cursors"/></summary>
                    public  List<string>    cursors;
        public   override   TaskType        TaskType => TaskType.closeCursors;
        
        public   override   string          TaskName => $"container: '{container}', cursor: {cursors}";

        public override Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var result = Execute(database, response, syncContext);
            return Task.FromResult(result);
        }
        
        public override SyncTaskResult Execute (EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container.IsNull()) {
                return MissingContainer();
            }
            var entityContainer     = database.GetOrCreateContainer(container);
            if (entityContainer == null) {
                return ContainerNotFound();
            }
            RemoveCursors(entityContainer, cursors, syncContext.User);
            
            var count = entityContainer.cursors.Count;
            return new CloseCursorsResult { count = count };
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
                if (cursor == null)
                    continue;
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
    /// <summary>
    /// Result of a <see cref="CloseCursors"/> task
    /// </summary>
    public sealed class CloseCursorsResult : SyncTaskResult
    {
        public              int             count;
        internal override   bool            Failed      => false;
        internal override   TaskType        TaskType    => TaskType.closeCursors;
    }
}