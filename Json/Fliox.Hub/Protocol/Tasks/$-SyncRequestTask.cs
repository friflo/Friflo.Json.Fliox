// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Polymorphic base type for all tasks.<br/>
    /// All tasks fall into two categories:<br/>
    /// <b>container operations</b> like: create, read, upsert, delete, query, ...<br/>
    /// <b>database operation</b> like sending commands or messages
    /// </summary>
    [Fri.Discriminator("task", Description = "task type")]
    [Fri.Polymorph(typeof(CreateEntities),          Discriminant = "create")]
    [Fri.Polymorph(typeof(UpsertEntities),          Discriminant = "upsert")]
    [Fri.Polymorph(typeof(ReadEntities),            Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntities),           Discriminant = "query")]
    [Fri.Polymorph(typeof(AggregateEntities),       Discriminant = "aggregate")]
    [Fri.Polymorph(typeof(PatchEntities),           Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntities),          Discriminant = "delete")]
    [Fri.Polymorph(typeof(SendMessage),             Discriminant = "message")]
    [Fri.Polymorph(typeof(SendCommand),             Discriminant = "command")]
    [Fri.Polymorph(typeof(CloseCursors),            Discriminant = "closeCursors")]
    [Fri.Polymorph(typeof(SubscribeChanges),        Discriminant = "subscribeChanges")]
    [Fri.Polymorph(typeof(SubscribeMessage),        Discriminant = "subscribeMessage")]
    [Fri.Polymorph(typeof(ReserveKeys),             Discriminant = "reserveKeys")]
    public abstract class SyncRequestTask
    {
                     public     JsonValue           info;
        [Fri.Ignore] public     int                 index;
        [Fri.Ignore] internal   JsonValue?          json;
        
        internal abstract Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext);
        internal abstract   TaskType                TaskType { get; }
        public   abstract   string                  TaskName { get; }

        public   override   string                  ToString() => TaskName;

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