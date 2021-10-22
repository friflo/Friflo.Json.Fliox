// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
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
    /// The list of these types is returned by <see cref="Types"/> 
    /// <br></br>
    /// Note: By applying this classification the protocol can also be used in peer-to-peer networking.
    /// 
    /// <br></br>
    /// General principle of the application communication of <see cref="Fliox"/>
    /// <para>
    ///     All messages like requests (their tasks), responses (their results) and events are stateless.
    ///     In other words: All messages are self-contained and doesnt (and must not) rely and previous sent messages.
    ///     This enables embedding all messages in various communication protocols like HTTP, WebSockets, TCP, WebRTC
    ///     or datagram based protocols.
    ///     This also means all <see cref="Fliox"/> messages doesnt (and must not) require a session.
    ///     This principle also enables using a single <see cref="FlioxHub"/> by multiple clients like
    ///     <see cref="Client.FlioxClient"/> even for remote clients like <see cref="RemoteClientHub"/>.
    /// </para>
    /// </summary>
    [Fri.Discriminator("msg")] 
    [Fri.Polymorph(typeof(SyncRequest),    Discriminant = "sync")]
    [Fri.Polymorph(typeof(SyncResponse),   Discriminant = "resp")]
    [Fri.Polymorph(typeof(ErrorResponse),  Discriminant = "error")]
    [Fri.Polymorph(typeof(EventMessage),   Discriminant = "ev")]
    public abstract class ProtocolMessage
    {
        internal abstract   MessageType     MessageType { get; }
        
        public static Type[] Types => new [] { typeof(ProtocolMessage), typeof(ProtocolRequest), typeof(ProtocolResponse), typeof(ProtocolEvent) }; 
    }
    
    // ----------------------------------- request -----------------------------------
    [Fri.Discriminator("msg")] 
    [Fri.Polymorph(typeof(SyncRequest),         Discriminant = "sync")]
    public abstract class ProtocolRequest   : ProtocolMessage {
        /// <summary>Used only for <see cref="RemoteClientHub"/> to enable:
        /// <para>
        ///   1. Out of order response handling for their corresponding requests.
        /// </para>
        /// <para>
        ///   2. Multiplexing of requests and their responses for multiple clients e.g. <see cref="Client.FlioxClient"/>
        ///      using the same connection.
        ///      This is not a common scenario but it enables using a single <see cref="WebSocketClientHub"/>
        ///      used by multiple clients.
        /// </para>
        /// The host itself only echos the <see cref="reqId"/> to <see cref="ProtocolResponse.reqId"/> and doesn't do
        /// anythings else with it.
        /// </summary>
        [Fri.Property(Name =               "req")]
                        public  int?        reqId       { get; set; }
        /// As a user can access an <see cref="FlioxHub"/> by multiple clients the <see cref="clientId"/>
        /// enables identifying each client individually.
        /// The <see cref="clientId"/> is used for <see cref="SubscribeMessage"/> and <see cref="SubscribeChanges"/>
        /// to enable sending <see cref="EventMessage"/>'s to the desired subscriber.
        [Fri.Property(Name =               "clt")]
                        public  JsonKey     clientId    { get; set; }
    }
    
    // ----------------------------------- response -----------------------------------
    [Fri.Discriminator("msg")] 
    [Fri.Polymorph(typeof(SyncResponse),        Discriminant = "resp")]
    [Fri.Polymorph(typeof(ErrorResponse),       Discriminant = "error")]
    public abstract class ProtocolResponse : ProtocolMessage {
        /// <summary>Set to the value of the corresponding <see cref="ProtocolRequest.reqId"/></summary>
        [Fri.Property(Name =               "req")]
                        public  int?        reqId       { get; set; }
        /// <summary>
        /// Set to <see cref="ProtocolRequest.clientId"/> of a <see cref="SyncRequest"/> in case the given
        /// <see cref="ProtocolRequest.clientId"/> was valid. Otherwise it is set to null.
        /// Calling <see cref="Auth.Authenticator.EnsureValidClientId"/> when <see cref="clientId"/> == null a
        /// new unique client id will be assigned.
        /// For tasks which require a <see cref="clientId"/> a client need to set <see cref="ProtocolRequest.clientId"/>
        /// to <see cref="clientId"/>.
        /// This enables tasks like <see cref="SubscribeMessage"/> or <see cref="SubscribeChanges"/> identifying the
        /// <see cref="EventMessage"/> target. 
        /// </summary>
        [Fri.Property(Name =               "clt")]
                        public  JsonKey     clientId    { get; set; }
    }
    
    // ----------------------------------- event -----------------------------------
    [Fri.Discriminator("msg")] 
    [Fri.Polymorph(typeof(EventMessage),   Discriminant = "ev")]
    public abstract class ProtocolEvent     : ProtocolMessage {
        // note for all fields
        // used { get; set; } to force properties on the top of JSON
        
        /// Increasing event sequence number starting with 1 for a specific target client <see cref="dstClientId"/>.
        /// Each target client (subscriber) has its own sequence.
                        public  int         seq         { get; set; }
        /// The user which caused the event. Specifically the user which made a database change or sent a message / command.
        /// The user client is not preserved by en extra property as a use case for this is not obvious.
        [Fri.Property(Name =               "src")]
        [Fri.Required]  public  JsonKey     srcUserId   { get; set; }
        
        /// The target client the event is sent to. This enabled sharing a single (WebSocket) connection by multiple clients.
        /// In many scenarios this property is redundant as every client uses a WebSocket exclusively.
        [Fri.Property(Name =               "clt")]
        [Fri.Required]  public  JsonKey     dstClientId { get; set; }
    }
    
    public enum MessageType
    {
        sub,
        sync,
        resp,
        error
    }
}