// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- request -----------------------------------
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(SyncRequest),         Discriminant = "sync")]
    public abstract class DatabaseRequest {
        internal abstract   RequestType  RequestType { get; }
    }
    
    // ----------------------------------- response -----------------------------------
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(SyncResponse),        Discriminant = "sync")]
    [Fri.Polymorph(typeof(ErrorResponse),       Discriminant = "error")]
    public abstract class DatabaseResponse {
        internal abstract   RequestType  RequestType { get; }
    }
    
    // ReSharper disable InconsistentNaming
    public enum RequestType {
        sync,
        error
    }
}