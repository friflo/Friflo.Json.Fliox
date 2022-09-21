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
        
        // [Ignore] internal    FilterOperation     filterOp;
        [Ignore]    internal    JsonFilter          jsonFilter;
        
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
                var reader          = pooled.instance.reader;
                var filterOperation = reader.Read<FilterOperation>(filter);
                jsonFilter          = filterOperation != null ? new JsonFilter(filterOperation) : null;
                if (reader.Error.ErrSet) {
                    return Task.FromResult<SyncTaskResult>(InvalidTask($"filterTree error: {reader.Error.msg.ToString()}"));
                }
            }
            
            var eventReceiver   = syncContext.eventReceiver;
            if (!eventDispatcher.SubscribeChanges(database.name, this, syncContext.User, syncContext.clientId, eventReceiver, out error))
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
    

    
    /// <summary>Filter type used to specify the type of an entity change</summary>
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