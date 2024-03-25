// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- message -----------------------------------
    /// <summary>
    /// <see cref="ProtocolMessage"/> is the base type for all messages which are classified into request, response and event.
    /// </summary>
    /// <remarks>
    /// A <see cref="ProtocolMessage"/> can also be used in communication going beyond the request / response schema.
    /// A <see cref="ProtocolMessage"/> is either one of the following types:
    /// <list type="bullet">
    ///   <item> <see cref="ProtocolRequest"/>  send by clients / received by hosts</item>
    ///   <item> <see cref="ProtocolResponse"/> send by hosts / received by clients</item>
    ///   <item> <see cref="ProtocolEvent"/>    send by hosts / received by clients</item>
    /// </list>
    /// <i>Note</i>: By applying this classification the protocol can also be used in peer-to-peer networking.
    /// <para>
    ///     General principle of <see cref="Fliox"/> message protocol:<br/>
    ///     All messages like requests (their tasks), responses (their results) and events are stateless.<br/>
    ///     In other words: All messages are self-contained and doesn't (and must not) rely and previous sent messages.
    ///     The technical aspect of having a connection e.g. HTTP or WebSocket is not relevant.
    ///     This enables two fundamental features:<br/>
    ///     1. embedding all messages in various communication protocols like HTTP, WebSockets, TCP, WebRTC or datagram based protocols.<br/>
    ///     2. multiplexing of messages from different clients, servers or peers in a shared connection.<br/>
    ///     This also means all <see cref="Fliox"/> messages doesn't (and must not) require a session.<br/>
    ///     This principle also enables using a single <see cref="FlioxHub"/> by multiple clients like
    ///     <see cref="Client.FlioxClient"/> even for remote clients like <see cref="Remote.SocketClientHub"/>.
    /// </para>
    /// </remarks>
    [Discriminator("msg", "message type")] 
    [PolymorphType(typeof(SyncRequest),     "sync")]
    [PolymorphType(typeof(SyncResponse),    "resp")]
    [PolymorphType(typeof(ErrorResponse),   "error")]
    [PolymorphType(typeof(EventMessage),    "ev")]
    public abstract class ProtocolMessage
    {
        internal abstract   MessageType     MessageType { get; }
        
        public static Type[] Types => new [] { typeof(ProtocolMessage), typeof(ProtocolRequest), typeof(ProtocolResponse), typeof(ProtocolEvent) }; 
    }
    
    // ----------------------------------- request -----------------------------------
    [Discriminator("msg", "request type")] 
    [PolymorphType(typeof(SyncRequest),         "sync")]
    public abstract class ProtocolRequest   : ProtocolMessage {
        /// <summary>Used only for <see cref="Remote.SocketClientHub"/> to enable:
        /// <para>
        ///   1. Out of order response handling for their corresponding requests.
        /// </para>
        /// <para>
        ///   2. Multiplexing of requests and their responses for multiple clients e.g. <see cref="Client.FlioxClient"/>
        ///      using the same connection.
        ///      This is not a common scenario but it enables using a single <see cref="Remote.WebSocketClientHub"/>
        ///      used by multiple clients.
        /// </para>
        /// The host itself only echos the <see cref="reqId"/> to <see cref="ProtocolResponse.reqId"/> and
        /// does <b>not</b> utilize it internally.
        /// </summary>
        [Serialize                        ("req")]
                        public  int?        reqId;
        /// <summary>As a user can access a <see cref="FlioxHub"/> by multiple clients the <see cref="clientId"/>
        /// enables identifying each client individually. <br/>
        /// The <see cref="clientId"/> is used for <see cref="SubscribeMessage"/> and <see cref="SubscribeChanges"/>
        /// to enable sending <see cref="SyncEvent"/>'s to the desired subscriber.
        /// </summary>
        [Serialize                        ("clt")]
                        public  ShortString clientId;
    }
    
    // ----------------------------------- response -----------------------------------
    /// <summary>
    /// Base type for response messages send from a host to a client in reply of <see cref="SyncRequest"/>
    /// </summary>
    /// <remarks>
    /// A response is either a <see cref="SyncResponse"/> or a <see cref="ErrorResponse"/> in case of a general error.
    /// </remarks> 
    [Discriminator("msg", "response type")] 
    [PolymorphType(typeof(SyncResponse),        "resp")]
    [PolymorphType(typeof(ErrorResponse),       "error")]
    public abstract class ProtocolResponse : ProtocolMessage {
        /// <summary>Set to the value of the corresponding <see cref="ProtocolRequest.reqId"/> of a <see cref="ProtocolRequest"/></summary>
        [Serialize                        ("req")]
                        public  int?        reqId;
        /// <summary>
        /// Set to <see cref="ProtocolRequest.clientId"/> of a <see cref="SyncRequest"/> in case the given
        /// <see cref="ProtocolRequest.clientId"/> was valid. Otherwise it is set to null.
        /// </summary>
        /// <remarks>
        /// Calling <see cref="Host.Auth.Authenticator.EnsureValidClientId"/> when <see cref="clientId"/> == null a
        /// new unique client id will be assigned. <br/>
        /// For tasks which require a <see cref="clientId"/> a client need to set <see cref="ProtocolRequest.clientId"/>
        /// to <see cref="clientId"/>. <br/>
        /// This enables tasks like <see cref="SubscribeMessage"/> or <see cref="SubscribeChanges"/> identifying the
        /// <see cref="SyncEvent"/> target. 
        /// </remarks>
        [Serialize                        ("clt")]
                        public  ShortString clientId;
    }
    
    // ----------------------------------- event -----------------------------------
    [Discriminator("msg", "event type")] 
    [PolymorphType(typeof(EventMessage),   "ev")]
    public abstract class ProtocolEvent : ProtocolMessage {
        /// <summary>
        /// The target client the event is sent to. This enables sharing a single (WebSocket) connection by multiple clients.
        /// In many scenarios this property is redundant as every client uses a WebSocket exclusively.
        /// </summary>
        [Serialize                        ("clt")]
                        public  ShortString dstClientId;
    }
    
    /// <summary>
    /// The general message types used in the Protocol
    /// </summary>
    public enum MessageType
    {
        None    = 0,
        /// <summary>event message - send from host to clients with subscriptions</summary>
        ev      = 1,
        /// <summary>request - send from a client to a host</summary>
        sync    = 2,
        /// <summary>response - send from a host to a client in reply of a request</summary>
        resp    = 3,
        /// <summary>response error - send from a host to a client in reply of a request</summary>
        error   = 4
    }
    
    /// <summary>
    /// Annotated fields are only available for debugging ergonomics.
    /// They are not not used by the library in any way as they represent redundant information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DebugInfoAttribute : Attribute { }
}