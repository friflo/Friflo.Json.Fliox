// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Sync
{
    // ----------------------------------- request -----------------------------------
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(SyncRequest),         Discriminant = "sync")]
    public abstract class DatabaseRequest {
        // ReSharper disable once InconsistentNaming
        /// <summary>Used only for <see cref="Database.Remote.RemoteClientDatabase"/> to enable:
        /// <para>
        ///   1. Out of order response handling for their corresponding requests.
        /// </para>
        /// <para>
        ///   2. Multiplexing of requests and their responses for multiple clients e.g. <see cref="Graph.EntityStore"/>
        ///      using the same connection.
        ///      This is not a common scenario but it enables using a single <see cref="Database.Remote.WebSocketClientDatabase"/>
        ///      used by multiple clients.
        /// </para>
        /// </summary>
        public              int?            reqId       { get; set; }
        internal abstract   RequestType     RequestType { get; }
    }
    
    // ----------------------------------- response -----------------------------------
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(SyncResponse),        Discriminant = "sync")]
    [Fri.Polymorph(typeof(ErrorResponse),       Discriminant = "error")]
    public abstract class DatabaseResponse {
        // ReSharper disable once InconsistentNaming
        /// <summary>Set to the value of the corresponding <see cref="DatabaseRequest.reqId"/></summary>
        public              int?            reqId       { get; set; }
        internal abstract   RequestType     RequestType { get; }
    }
    
    // ReSharper disable InconsistentNaming
    public enum RequestType {
        sync,
        error
    }
}