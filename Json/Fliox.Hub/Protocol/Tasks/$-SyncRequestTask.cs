// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Polymorphic base type for all tasks.<br/>
    /// All tasks fall into two categories:<br/>
    /// <b>container operations</b> like: create, read, upsert, delete, query, ...<br/>
    /// <b>database operation</b> like sending commands or messages
    /// </summary>
    [Discriminator("task", "task type")]
    [PolymorphType(typeof(CreateEntities),          "create")]
    [PolymorphType(typeof(UpsertEntities),          "upsert")]
    [PolymorphType(typeof(ReadEntities),            "read")]
    [PolymorphType(typeof(QueryEntities),           "query")]
    [PolymorphType(typeof(AggregateEntities),       "aggregate")]
    [PolymorphType(typeof(MergeEntities),           "merge")]
    [PolymorphType(typeof(DeleteEntities),          "delete")]
    [PolymorphType(typeof(SendMessage),             "message")]
    [PolymorphType(typeof(SendCommand),             "command")]
    [PolymorphType(typeof(CloseCursors),            "closeCursors")]
    [PolymorphType(typeof(SubscribeChanges),        "subscribeChanges")]
    [PolymorphType(typeof(SubscribeMessage),        "subscribeMessage")]
    [PolymorphType(typeof(ReserveKeys),             "reserveKeys")]
    public abstract class SyncRequestTask
    {
                    public     JsonValue    info;
        [Ignore]    internal   int          index;
        [Ignore]    internal   bool         synchronous;
        /// <summary>cached JSON of this <see cref="SyncRequestTask"/> instance serialized as JSON</summary>
        [Ignore]    internal   JsonValue?   json;
        [Ignore]    internal   SyncTask     syncTask;
        
        public      abstract   Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext);
        // todo make abstract when SyncRequestTask are implemented
        public      virtual    SyncTaskResult       Execute     (EntityDatabase database, SyncResponse response, SyncContext syncContext) => throw new NotImplementedException();
        
        public      abstract   TaskType     TaskType { get; }
        public      abstract   string       TaskName { get; }

        public      override   string       ToString() => TaskName;

        internal static TaskErrorResult TaskError(CommandError error) {
            return new TaskErrorResult (error.type, error.message);   
        }
        
        internal static TaskErrorResult InvalidTaskError(string error) {
            // error = $"{message} {TaskType} ({TaskName})";
            return new TaskErrorResult (TaskErrorResultType.InvalidTask, error);   
        }
        
        internal static TaskErrorResult MissingField(string field) {
            return InvalidTaskError($"missing field: {field}");   
        }
        
        internal static TaskErrorResult MissingContainer() {
            return InvalidTaskError($"missing field: container");   
        }
        
        internal static TaskErrorResult InvalidTask(string error) {
            return InvalidTaskError(error);
        }
        
        public static TaskErrorResult PermissionDenied(string message) {
            // message = $"{message} {TaskType} ({TaskName})";
            return new TaskErrorResult (TaskErrorResultType.PermissionDenied, message);
        }

        internal static bool ValidReferences(List<References> references, out TaskErrorResult error) {
            if (references == null) {
                error = null;
                return true;
            }
            foreach (var reference in references) {
                if (reference.selector == null) {
                    error = InvalidTaskError("missing reference selector");
                    return false;
                }
                if (reference.container == null) {
                    error =  InvalidTaskError("missing reference container");
                    return false;
                }
                var subReferences = reference.references;
                if (subReferences != null) {
                    if (!ValidReferences(subReferences, out error))
                        return false;
                }
            }
            error = null;
            return true;
        }
    }
}