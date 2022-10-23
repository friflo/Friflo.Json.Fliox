// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task result -----------------------------------
    [Discriminator("task", "task result type")]
    [PolymorphType(typeof(CreateEntitiesResult),    "create")]
    [PolymorphType(typeof(UpsertEntitiesResult),    "upsert")]
    [PolymorphType(typeof(ReadEntitiesResult),      "read")]
    [PolymorphType(typeof(QueryEntitiesResult),     "query")]
    [PolymorphType(typeof(AggregateEntitiesResult), "aggregate")]
    [PolymorphType(typeof(MergeEntitiesResult),     "merge")]
    [PolymorphType(typeof(DeleteEntitiesResult),    "delete")]
    [PolymorphType(typeof(SendMessageResult),       "message")]
    [PolymorphType(typeof(SendCommandResult),       "command")]
    [PolymorphType(typeof(CloseCursorsResult),      "closeCursors")]
    [PolymorphType(typeof(SubscribeChangesResult),  "subscribeChanges")]
    [PolymorphType(typeof(SubscribeMessageResult),  "subscribeMessage")]
    [PolymorphType(typeof(ReserveKeysResult),       "reserveKeys")]
    //
    [PolymorphType(typeof(TaskErrorResult),         "error")]
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
        merge,
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