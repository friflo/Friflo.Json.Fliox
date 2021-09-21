// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SubscribeChanges : SyncTask
    {
        [Fri.Required]  public  string          container;
        [Fri.Required]  public  List<Change>    changes;
                        public  FilterOperation filter;
        
        internal override       TaskType        TaskType  => TaskType.subscribeChanges;
        public   override       string          TaskName  => $"container: '{container}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var eventBroker = database.eventBroker;
            if (eventBroker == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("database has no eventBroker"));
            if (messageContext.clientId == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("subscribe task requires client id set in sync request"));
            if (container == null)
                return Task.FromResult<SyncTaskResult>(MissingContainer());
            if (changes == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(changes)));
            
            var eventTarget = messageContext.eventTarget;
            if (eventTarget == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("caller/request doesnt provide a eventTarget"));
            
            eventBroker.SubscribeChanges(this, messageContext.clientId, eventTarget);
            return Task.FromResult<SyncTaskResult>(new SubscribeChangesResult());
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SubscribeChangesResult : SyncTaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeChanges;
    }
    
    /// <summary>Contains predefined sets of common database <see cref="Change"/> filters.</summary>
    public static class Changes
    {
        /// <summary>Shortcut to subscribe to all types database changes. These ase <see cref="Change.create"/>,
        /// <see cref="Change.upsert"/>, <see cref="Change.patch"/> and <see cref="Change.delete"/></summary>
        public static readonly ReadOnlyCollection<Change> All  = new List<Change> { Change.create, Change.upsert, Change.delete, Change.patch }.AsReadOnly();
        /// <summary>Shortcut to unsubscribe from all database change types.</summary>
        public static readonly ReadOnlyCollection<Change> None = new List<Change>().AsReadOnly();
    }
    
    /// <summary>
    /// Filters used to specify the type of a database change.
    /// Consider using the predefined sets <see cref="Changes.All"/> or <see cref="Changes.None"/> as shortcuts.
    /// </summary>
    // ReSharper disable InconsistentNaming
    public enum Change
    {
        /// <summary>Filter database change events of new created entities.</summary>
        create,
        /// <summary>Filter database change events of upserted entities.</summary>
        upsert,
        /// <summary>Filter database change events used to patch entities.</summary>
        patch,
        /// <summary>Filter database change events used to delete entities.</summary>
        delete
    }
}