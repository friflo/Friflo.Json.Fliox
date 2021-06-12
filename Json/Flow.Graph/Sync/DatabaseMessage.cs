// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Sync
{
    /// <summary>
    /// A container for different types of a message classified into request, response and event.
    /// It is used in communication protocols which support more than the request / response schema.
    /// Only one of its fields is set at a time.
    /// More at: <see cref="Database.Remote.ProtocolType"/>
    /// <br></br>
    /// Note: By applying this classification the protocol can also be used in peer-to-peer networking.
    /// 
    /// <para>
    ///     <see cref="DatabaseMessage"/>'s are used for WebSocket communication to notify multiple
    ///     <see cref="DatabaseEvent"/> to a client having sent only a single subscription request upfront.
    /// </para>
    /// <para>
    ///     <see cref="DatabaseMessage"/>' are not applicable for HTTP as HTTP support only a
    ///     request / response pattern and has no mechanism to implement <see cref="DatabaseEvent"/>'s.
    /// </para>
    /// </summary>
    public class DatabaseMessage
    {
        /// <summary>A request message</summary>
        public DatabaseRequest  req;
        /// <summary>A response message</summary>
        public DatabaseResponse resp;
        /// <summary>An event message</summary>
        public DatabaseEvent    ev;
    }
}