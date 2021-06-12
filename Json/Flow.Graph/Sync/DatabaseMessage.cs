// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Sync
{
    /// <summary>
    /// A container for different types of message classified into request, response and event messages.
    /// It is used in communication protocols which support more than request/response schema.
    /// Only one of its fields is set at a time.
    /// 
    /// <para>
    ///     <see cref="DatabaseMessage"/>'s are used for WebSocket communication to notify about event messages having
    ///     no corresponding request message.
    /// </para>
    /// <para>
    ///     <see cref="DatabaseMessage"/>' are not not used for HTTP communication as HTTP support request / response
    ///     pattern by itself and has not support for events. 
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