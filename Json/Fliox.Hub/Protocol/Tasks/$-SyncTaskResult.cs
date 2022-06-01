// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task result -----------------------------------
    [Discriminator("task", Description = "task result type")]
    [PolymorphType(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [PolymorphType(typeof(UpsertEntitiesResult),    Discriminant = "upsert")]
    [PolymorphType(typeof(ReadEntitiesResult),      Discriminant = "read")]
    [PolymorphType(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [PolymorphType(typeof(AggregateEntitiesResult), Discriminant = "aggregate")]
    [PolymorphType(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    [PolymorphType(typeof(DeleteEntitiesResult),    Discriminant = "delete")]
    [PolymorphType(typeof(SendMessageResult),       Discriminant = "message")]
    [PolymorphType(typeof(SendCommandResult),       Discriminant = "command")]
    [PolymorphType(typeof(CloseCursorsResult),      Discriminant = "closeCursors")]
    [PolymorphType(typeof(SubscribeChangesResult),  Discriminant = "subscribeChanges")]
    [PolymorphType(typeof(SubscribeMessageResult),  Discriminant = "subscribeMessage")]
    [PolymorphType(typeof(ReserveKeysResult),       Discriminant = "reserveKeys")]
    //
    [PolymorphType(typeof(TaskErrorResult),         Discriminant = "error")]
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