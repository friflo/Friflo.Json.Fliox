// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    // ------------------------------ DatabaseTask ------------------------------
    [Fri.Discriminator("task")]
    [Fri.Polymorph(typeof(CreateEntities),          Discriminant = "create")]
    [Fri.Polymorph(typeof(UpdateEntities),          Discriminant = "update")]
    [Fri.Polymorph(typeof(ReadEntitiesList),        Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntities),           Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntities),           Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntities),          Discriminant = "delete")]
    [Fri.Polymorph(typeof(Echo),                    Discriminant = "echo")]
    [Fri.Polymorph(typeof(SubscribeMessages),       Discriminant = "subscribe")]
    public abstract class DatabaseTask
    {
        [Fri.Ignore]
        public              int                     index;              
            
        internal abstract   Task<TaskResult>        Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext);
        internal abstract   TaskType                TaskType { get; }
        
        internal static TaskErrorResult TaskError(CommandError error) {
            return new TaskErrorResult {type = TaskErrorResultType.DatabaseError, message = error.message};   
        }
        
        private TaskErrorResult InvalidTaskError(string error) {
            var message = $"{error} - tasks[{index}]: {TaskType}";
            return new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = message};   
        }
        
        internal TaskErrorResult MissingField(string field) {
            return InvalidTaskError($"missing field: {field}");   
        }
        
        internal TaskErrorResult MissingContainer() {
            return InvalidTaskError($"missing field: container");   
        }
        
        internal TaskErrorResult InvalidTask(string error) {
            return InvalidTaskError(error);
        }

        internal bool ValidReferences(List<References> references, out TaskErrorResult error) {
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
    
    // ------------------------------ TaskResult ------------------------------
    [Fri.Discriminator("task")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(UpdateEntitiesResult),    Discriminant = "update")]
    [Fri.Polymorph(typeof(ReadEntitiesListResult),  Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntitiesResult),    Discriminant = "delete")]
    [Fri.Polymorph(typeof(EchoResult),              Discriminant = "echo")]
    [Fri.Polymorph(typeof(SubscribeMessagesResult), Discriminant = "subscribe")]
    //
    [Fri.Polymorph(typeof(TaskErrorResult),         Discriminant = "error")]
    public abstract class TaskResult
    {
        internal abstract TaskType          TaskType { get; }
    }
    
    // ReSharper disable InconsistentNaming
    public enum TaskType
    {
        read,
        query,
        create,
        update,
        patch,
        delete,
        echo,
        subscribe,
        //
        error
    }
}