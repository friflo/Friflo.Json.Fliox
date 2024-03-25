// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    [PolymorphType(typeof(SendMessageResult),       "msg")]
    [PolymorphType(typeof(SendCommandResult),       "cmd")]
    [PolymorphType(typeof(CloseCursorsResult),      "closeCursors")]
    [PolymorphType(typeof(SubscribeChangesResult),  "subscribeChanges")]
    [PolymorphType(typeof(SubscribeMessageResult),  "subscribeMessage")]
    [PolymorphType(typeof(ReserveKeysResult),       "reserveKeys")]
    //
    [PolymorphType(typeof(TaskErrorResult),         "error")]
    public abstract class SyncTaskResult
    {
        internal abstract TaskType          TaskType { get; }
        internal abstract bool              Failed   { get; }
    }
    
    // ReSharper disable InconsistentNaming
    /// <summary>Type of a task that operates on the database or a container</summary>
    public enum TaskType
    {
        /// <summary>read container entities by id</summary>
        read                =  1,
        /// <summary>query container entities using a filter</summary>
        query               =  2,
        /// <summary>create container entities</summary>
        create              =  3,
        /// <summary>upsert container entities</summary>
        upsert              =  4,
        /// <summary>patch container entities by id</summary>
        merge               =  5,
        /// <summary>delete container entities by id</summary>
        delete              =  6,
        /// <summary>aggregate - count - container entities using a filter</summary>
        aggregate           =  7,
        /// <summary>send a database message</summary>
        message             =  8,
        /// <summary>send a database command</summary>
        command             =  9,
        /// <summary>close cursors of a container</summary>
        closeCursors        = 10,
        /// <summary>subscribe to entity changes of a container</summary>
        subscribeChanges    = 11,
        /// <summary>subscribe to messages and commands send to a database</summary>
        subscribeMessage    = 12,
        /// <summary>wip</summary>
        reserveKeys         = 13,
        /// <summary>indicate an error when task was executed</summary>
        error               = 14
    }
}