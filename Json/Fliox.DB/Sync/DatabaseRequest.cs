// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- request -----------------------------------
    public abstract class DatabaseRequest : DatabaseMessage {
        // ReSharper disable once InconsistentNaming
        /// <summary>Used only for <see cref="NoSQL.Remote.RemoteClientDatabase"/> to enable:
        /// <para>
        ///   1. Out of order response handling for their corresponding requests.
        /// </para>
        /// <para>
        ///   2. Multiplexing of requests and their responses for multiple clients e.g. <see cref="Graph.EntityStore"/>
        ///      using the same connection.
        ///      This is not a common scenario but it enables using a single <see cref="NoSQL.Remote.WebSocketClientDatabase"/>
        ///      used by multiple clients.
        /// </para>
        /// </summary>
        public              int?            reqId       { get; set; }
    }
    
    // ----------------------------------- response -----------------------------------
    public abstract class DatabaseResponse : DatabaseMessage {
        // ReSharper disable once InconsistentNaming
        /// <summary>Set to the value of the corresponding <see cref="DatabaseRequest.reqId"/></summary>
        public              int?            reqId       { get; set; }
    }
}