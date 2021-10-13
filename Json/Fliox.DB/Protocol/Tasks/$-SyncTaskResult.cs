// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol.Tasks
{
    // ----------------------------------- task result -----------------------------------
    [Fri.Discriminator("task")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(UpsertEntitiesResult),    Discriminant = "upsert")]
    [Fri.Polymorph(typeof(ReadEntitiesListResult),  Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntitiesResult),    Discriminant = "delete")]
    [Fri.Polymorph(typeof(SendMessageResult),       Discriminant = "message")]
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
        message,
        subscribeChanges,
        subscribeMessage,
        reserveKeys,
        //
        error
    }
}