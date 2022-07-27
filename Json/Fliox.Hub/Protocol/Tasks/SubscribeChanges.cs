// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Subscribe to specific <see cref="changes"/> of the specified <see cref="container"/> using the given <see cref="filter"/> 
    /// </summary>
    public sealed class SubscribeChanges : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Required]  public      string              container;
        /// <summary>subscribe to entity <see cref="changes"/> of the given <see cref="container"/></summary>
        [Required]  public      List<EntityChange>  changes;
        /// <summary>subscription filter as a <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions">Lambda expression</a> (infix notation)
        /// returning a boolean value. E.g. <c>o.name == 'Smartphone'</c></summary>
                    public      JsonValue           filter;
                        
        [Ignore]    internal    FilterOperation     filterOp;
        
        internal override       TaskType            TaskType  => TaskType.subscribeChanges;
        public   override       string              TaskName  => $"container: '{container}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var hub             = syncContext.Hub;
            var eventDispatcher = hub.EventDispatcher;
            if (eventDispatcher == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("Hub has no EventDispatcher"));
            if (container == null)
                return Task.FromResult<SyncTaskResult>(MissingContainer());
            if (changes == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(changes)));
            
            if (!hub.Authenticator.EnsureValidClientId(hub.ClientController, syncContext, out string error))
                return Task.FromResult<SyncTaskResult>(InvalidTask(error));

            using (var pooled = syncContext.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                filterOp    = reader.Read<FilterOperation>(filter);
                if (reader.Error.ErrSet) {
                    return Task.FromResult<SyncTaskResult>(InvalidTask($"filterTree error: {reader.Error.msg.ToString()}"));
                }
            }
            
            var eventReceiver   = syncContext.eventReceiver;
            var eventAck        = syncContext.eventAck ?? 0;
            if (!eventDispatcher.SubscribeChanges(database.name, this, syncContext.User, syncContext.clientId, eventAck, eventReceiver, out error))
                return Task.FromResult<SyncTaskResult>(InvalidTask(error));
            
            return Task.FromResult<SyncTaskResult>(new SubscribeChangesResult());
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="SubscribeChanges"/> task
    /// </summary>
    public sealed class SubscribeChangesResult : SyncTaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeChanges;
    }
    
    /// <summary>Contains predefined sets of common database <see cref="EntityChange"/> filters.</summary>
    public static class ChangeFlags
    {
        /// <summary>Shortcut to subscribe to all types of database changes. These ase <see cref="EntityChange.create"/>,
        /// <see cref="EntityChange.upsert"/>, <see cref="EntityChange.patch"/> and <see cref="EntityChange.delete"/></summary>
        public const EntityChange All = EntityChange.create | EntityChange.upsert | EntityChange.delete | EntityChange.patch;

        /// <summary>Shortcut to unsubscribe from all database change types.</summary>
        public const EntityChange None = 0;
        
        internal static IReadOnlyList<EntityChange> ToList(EntityChange change) {
            var list = new List<EntityChange>(4);
            if ((change & EntityChange.create) != 0) list.Add(EntityChange.create);
            if ((change & EntityChange.upsert) != 0) list.Add(EntityChange.upsert);
            if ((change & EntityChange.delete) != 0) list.Add(EntityChange.delete);
            if ((change & EntityChange.patch)  != 0) list.Add(EntityChange.patch);
            return list;
        }
    }
    
    /// <summary>Filter type used to specify the type of a database change.</summary>
    /// <remarks>
    /// Consider using the predefined sets <see cref="ChangeFlags.All"/> or <see cref="ChangeFlags.None"/> as shortcuts.
    /// </remarks>
    // ReSharper disable InconsistentNaming
    [Flags]
    public enum EntityChange
    {
        /// <summary>filter change events of created entities.</summary>
        create  = 1,
        /// <summary>filter change events of upserted entities.</summary>
        upsert  = 2,
        /// <summary>filter change events of entity patches.</summary>
        patch   = 4,
        /// <summary>filter change events of deleted entities.</summary>
        delete  = 8
    }
}