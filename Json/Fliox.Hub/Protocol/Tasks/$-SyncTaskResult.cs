// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task result -----------------------------------
    [Fri.Discriminator("task", Description = "task result type")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(UpsertEntitiesResult),    Discriminant = "upsert")]
    [Fri.Polymorph(typeof(ReadEntitiesResult),      Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [Fri.Polymorph(typeof(AggregateEntitiesResult), Discriminant = "aggregate")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntitiesResult),    Discriminant = "delete")]
    [Fri.Polymorph(typeof(SendMessageResult),       Discriminant = "message")]
    [Fri.Polymorph(typeof(SendCommandResult),       Discriminant = "command")]
    [Fri.Polymorph(typeof(CloseCursorsResult),      Discriminant = "closeCursors")]
    [Fri.Polymorph(typeof(SubscribeChangesResult),  Discriminant = "subscribeChanges")]
    [Fri.Polymorph(typeof(SubscribeMessageResult),  Discriminant = "subscribeMessage")]
    [Fri.Polymorph(typeof(ReserveKeysResult),       Discriminant = "reserveKeys")]
    //
    [Fri.Polymorph(typeof(TaskErrorResult),         Discriminant = "error")]
    public abstract class SyncTaskResult
    {
        internal abstract TaskType          TaskType { get; }
    }
    
    // ReSharper disable InconsistentNaming
    /// <summary>Type of a task that operates on the database or a container</summary>
    public enum TaskType
    {
        /// <summary>read container entities by id</summary>
        read,
        /// <summary>query container entities using a filter</summary>
        query,
        /// <summary>create container entities</summary>
        create,
        /// <summary>upsert container entities</summary>
        upsert,
        /// <summary>patch container entities by id</summary>
        patch,
        /// <summary>delete container entities by id</summary>
        delete,
        /// <summary>aggregate - count - container entities using a filter</summary>
        aggregate,
        /// <summary>send a database message</summary>
        message,
        /// <summary>send a database command</summary>
        command,
        /// <summary>close cursors of a container</summary>
        closeCursors,
        /// <summary>subscribe to entity changes of a container</summary>
        subscribeChanges,
        /// <summary>subscribe to messages and commands send to a database</summary>
        subscribeMessage,
        /// <summary>wip</summary>
        reserveKeys,
        /// <summary>indicate an error when task was executed</summary>
        error
    }
}