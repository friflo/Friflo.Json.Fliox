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
    public abstract class DatabaseTask
    {
        [Fri.Ignore]
        public              int                     index;              
            
        internal abstract   Task<TaskResult>        Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext);
        internal abstract   TaskType                TaskType { get; }
        
        private string TaskError(string error) {
            return $"{error} - tasks[{index}]: {TaskType}";
        }

        internal static TaskErrorResult TaskError(CommandError error) {
            return new TaskErrorResult {type = TaskErrorResultType.DatabaseError, message = error.message};   
        }
        
        internal TaskErrorResult MissingField(string field) {
            return new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = TaskError($"missing field: {field}")};   
        }
        
        internal TaskErrorResult MissingContainer() {
            return new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = TaskError("missing field: container")};   
        }
        
        internal TaskErrorResult InvalidTask(string error) {
            return new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = TaskError(error)};
        }

        internal bool ValidReferences(List<References> references, out TaskErrorResult error) {
            if (references == null) {
                error = null;
                return true;
            }
            foreach (var reference in references) {
                if (reference.selector == null) {
                    error = new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = TaskError("missing reference selector")};
                    return false;
                }
                if (reference.container == null) {
                    error =  new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = TaskError("missing reference container")};
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
        //
        error
    }
}