// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- message -----------------------------------
    /// <summary>
    /// A container for different types of a message classified into request, response and event.
    /// It is used in communication protocols which support more than the request / response schema.
    /// Only one of its fields is set at a time.
    /// More at: <see cref="NoSQL.Remote.ProtocolType"/>
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
    /// <br></br>
    /// General principle of the application communication of <see cref="Fliox"/>
    /// <para>
    ///     All messages like requests (their tasks), responses (their results) and events are stateless.
    ///     In other words: All messages are self-contained and doesnt (and must not) rely and previous sent messages.
    ///     This enables embedding all messages in various communication protocols like HTTP, WebSockets, TCP
    ///     or datagram based protocols.
    ///     This also means all <see cref="Fliox"/> messages doesnt (and must not) require a session.
    ///     This principle also enables using a single <see cref="NoSQL.EntityDatabase"/> by multiple clients like
    ///     <see cref="Graph.EntityStore"/> even for remote clients like <see cref="NoSQL.Remote.RemoteClientDatabase"/>.
    /// </para>
    /// </summary>
    [Fri.Discriminator("type")] 
    [Fri.Polymorph(typeof(SubscriptionEvent),   Discriminant = "sub")]
    [Fri.Polymorph(typeof(SyncRequest),         Discriminant = "sync")]
    [Fri.Polymorph(typeof(SyncResponse),        Discriminant = "syncRes")]
    [Fri.Polymorph(typeof(ErrorResponse),       Discriminant = "error")]
    public abstract class DatabaseMessage
    {
        internal abstract   MessageType     MessageType { get; }
    }
    
    // ----------------------------------- request -----------------------------------
    public abstract class DatabaseRequest   : DatabaseMessage {
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
                                        public          int?            reqId       { get; set; }
    }
    
    // ----------------------------------- response -----------------------------------
    public abstract class DatabaseResponse : DatabaseMessage {
        // ReSharper disable once InconsistentNaming
        /// <summary>Set to the value of the corresponding <see cref="DatabaseRequest.reqId"/></summary>
                                        public          int?            reqId       { get; set; }
    }
    
    // ----------------------------------- event -----------------------------------
    public abstract class DatabaseEvent     : DatabaseMessage {
        // note for all fields
        // used { get; set; } to force properties on the top of JSON
        
        /// Increasing event sequence number starting with 1.
        /// Each target (subscriber) has its own sequence.  
                                        public  int     seq      { get; set; }
        /// The target the event is sent to
        [Fri.Property(Name = "target")] public  string  targetId { get; set; }
        /// The client which caused the event. Specifically the client which made a database change.
        [Fri.Property(Name = "client")] public  string  clientId { get; set; }
    }
    
    public enum MessageType
    {
        subscription,
        sync,
        resp,
        error
    }
}