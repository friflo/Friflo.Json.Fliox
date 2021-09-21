// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- message -----------------------------------
    /// <summary>
    /// <see cref="ProtocolMessage"/> is the base type for all messages which are classified into request, response and event.
    /// It can be used in communication protocols which support more than the request / response schema.
    /// <br/>
    /// A <see cref="ProtocolMessage"/> is either one of the following types:
    /// <list type="bullet">
    ///   <item> <see cref="ProtocolRequest"/>  send by clients / received by hosts</item>
    ///   <item> <see cref="ProtocolResponse"/> send by hosts / received by clients</item>
    ///   <item> <see cref="ProtocolEvent"/>    send by hosts / received by clients</item>
    /// </list>   
    /// <br></br>
    /// Note: By applying this classification the protocol can also be used in peer-to-peer networking.
    /// 
    /// <br></br>
    /// General principle of the application communication of <see cref="Fliox"/>
    /// <para>
    ///     All messages like requests (their tasks), responses (their results) and events are stateless.
    ///     In other words: All messages are self-contained and doesnt (and must not) rely and previous sent messages.
    ///     This enables embedding all messages in various communication protocols like HTTP, WebSockets, TCP
    ///     or datagram based protocols.
    ///     This also means all <see cref="Fliox"/> messages doesnt (and must not) require a session.
    ///     This principle also enables using a single <see cref="Host.EntityDatabase"/> by multiple clients like
    ///     <see cref="Graph.EntityStore"/> even for remote clients like <see cref="Friflo.Json.Fliox.DB.Remote.RemoteClientDatabase"/>.
    /// </para>
    /// </summary>
    [Fri.Discriminator("type")] 
    [Fri.Polymorph(typeof(SyncRequest),         Discriminant = "sync")]
    [Fri.Polymorph(typeof(SyncResponse),        Discriminant = "syncRes")]
    [Fri.Polymorph(typeof(SubscriptionEvent),   Discriminant = "sub")]
    [Fri.Polymorph(typeof(ErrorResponse),       Discriminant = "error")]
    public abstract class ProtocolMessage
    {
        internal abstract   MessageType     MessageType { get; }
    }
    
    // ----------------------------------- request -----------------------------------
    public abstract class ProtocolRequest   : ProtocolMessage {
        // ReSharper disable once InconsistentNaming
        /// <summary>Used only for <see cref="Friflo.Json.Fliox.DB.Remote.RemoteClientDatabase"/> to enable:
        /// <para>
        ///   1. Out of order response handling for their corresponding requests.
        /// </para>
        /// <para>
        ///   2. Multiplexing of requests and their responses for multiple clients e.g. <see cref="Graph.EntityStore"/>
        ///      using the same connection.
        ///      This is not a common scenario but it enables using a single <see cref="Friflo.Json.Fliox.DB.Remote.WebSocketClientDatabase"/>
        ///      used by multiple clients.
        /// </para>
        /// </summary>
                                        public          int?            reqId       { get; set; }
    }
    
    // ----------------------------------- response -----------------------------------
    public abstract class ProtocolResponse : ProtocolMessage {
        // ReSharper disable once InconsistentNaming
        /// <summary>Set to the value of the corresponding <see cref="ProtocolRequest.reqId"/></summary>
                                        public          int?            reqId       { get; set; }
    }
    
    // ----------------------------------- event -----------------------------------
    public abstract class ProtocolEvent     : ProtocolMessage {
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