// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
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
        internal abstract   Task<TaskResult>        Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext);
        internal abstract   TaskType                TaskType { get; }

        internal static TaskErrorResult TaskError(CommandError error) {
            return new TaskErrorResult {type = TaskErrorResultType.DatabaseError, message = error.message};   
        }
        
        internal TaskErrorResult MissingField(string field) {
            return new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = $"invalid task: {TaskType} - missing field: {field}"};   
        }
        
        internal TaskErrorResult MissingContainer() {
            return new TaskErrorResult {type = TaskErrorResultType.InvalidTask, message = $"invalid task: {TaskType} - missing field: container"};   
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