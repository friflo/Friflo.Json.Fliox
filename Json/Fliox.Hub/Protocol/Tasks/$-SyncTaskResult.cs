// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task result -----------------------------------
    [Fri.Discriminator("task")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(UpsertEntitiesResult),    Discriminant = "upsert")]
    [Fri.Polymorph(typeof(ReadEntitiesResult),      Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [Fri.Polymorph(typeof(AggregateEntitiesResult), Discriminant = "aggregate")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntitiesResult),    Discriminant = "delete")]
    [Fri.Polymorph(typeof(SendMessageResult),       Discriminant = "message")]
    [Fri.Polymorph(typeof(SendCommandResult),       Discriminant = "command")]
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
    public enum TaskType
    {
        read,
        query,
        create,
        upsert,
        patch,
        delete,
        aggregate,
        message,
        command,
        subscribeChanges,
        subscribeMessage,
        reserveKeys,
        //
        error
    }
}