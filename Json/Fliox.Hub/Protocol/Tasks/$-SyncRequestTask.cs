// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Ignore = Friflo.Json.Fliox.IgnoreMemberAttribute;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Polymorphic base type for all tasks.<br/>
    /// All tasks fall into two categories:<br/>
    /// <b>container operations</b> like: create, read, upsert, delete, query, ...<br/>
    /// <b>database operation</b> like sending commands or messages
    /// </summary>
    [Discriminator("task", Description = "task type")]
    [PolymorphType(typeof(CreateEntities),          Discriminant = "create")]
    [PolymorphType(typeof(UpsertEntities),          Discriminant = "upsert")]
    [PolymorphType(typeof(ReadEntities),            Discriminant = "read")]
    [PolymorphType(typeof(QueryEntities),           Discriminant = "query")]
    [PolymorphType(typeof(AggregateEntities),       Discriminant = "aggregate")]
    [PolymorphType(typeof(PatchEntities),           Discriminant = "patch")]
    [PolymorphType(typeof(DeleteEntities),          Discriminant = "delete")]
    [PolymorphType(typeof(SendMessage),             Discriminant = "message")]
    [PolymorphType(typeof(SendCommand),             Discriminant = "command")]
    [PolymorphType(typeof(CloseCursors),            Discriminant = "closeCursors")]
    [PolymorphType(typeof(SubscribeChanges),        Discriminant = "subscribeChanges")]
    [PolymorphType(typeof(SubscribeMessage),        Discriminant = "subscribeMessage")]
    [PolymorphType(typeof(ReserveKeys),             Discriminant = "reserveKeys")]
    public abstract class SyncRequestTask
    {
                    public     JsonValue    info;
        [Ignore]    public     int          index;
        [Ignore]    internal   JsonValue?   json;
        
        internal abstract Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext);
        internal    abstract   TaskType     TaskType { get; }
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