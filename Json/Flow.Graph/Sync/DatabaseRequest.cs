// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(SyncRequest),         Discriminant = "sync")]
 // [Fri.Polymorph(typeof(SubscribeRequest),    Discriminant = "subscribe")]
    public abstract class DatabaseRequest {
        internal abstract   RequestType  RequestType { get; }
    }
    
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(SyncResponse),        Discriminant = "sync")]
 // [Fri.Polymorph(typeof(SubscribeResponse),   Discriminant = "subscribe")]
    [Fri.Polymorph(typeof(ResponseError),       Discriminant = "error")]
    public abstract class DatabaseResponse {
        internal abstract   RequestType  RequestType { get; }
    }
    
    /* public class SubscribeRequest : DatabaseRequest
    {
        internal override   RequestType  RequestType => RequestType.subscribe;
    }
    
    public class SubscribeResponse : DatabaseResponse
    {
        internal override   RequestType  RequestType => RequestType.subscribe;
    } */
    
    // ReSharper disable InconsistentNaming
    public enum RequestType {
        sync,
        // subscribe,
        error
    }
}