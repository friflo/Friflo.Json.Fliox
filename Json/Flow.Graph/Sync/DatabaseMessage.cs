// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Sync
{
    /// <summary>
    /// A container for different types of a message classified into request, response and event.
    /// It is used in communication protocols which support more than the request / response schema.
    /// Only one of its fields is set at a time.
    /// <br></br>
    /// Note: By applying this classification the protocol can also be used in peer-to-peer networking.
    /// 
    /// <para>
    ///     <see cref="DatabaseMessage"/>'s are used for WebSocket communication to notify multiple events to a client
    ///     having sent only a single subscription request upfront.
    /// </para>
    /// <para>
    ///     <see cref="DatabaseMessage"/>' are not used for HTTP communication as HTTP support request / response
    ///     pattern by itself and has no mechanism to implements event messages.
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