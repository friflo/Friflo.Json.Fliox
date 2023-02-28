// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// An <see cref="EventReceiver"/> is used to send events to clients they have subscribed before.<br/>
    /// A single <see cref="EventReceiver"/> can be shared by multiple clients to enable using a single
    /// remote connection. To address a specific client in case of a shared remote connection the
    /// <see cref="ClientEvent.dstClientId"/> is used.
    /// </summary>
    public abstract class EventReceiver {
        /// <summary>The endpoint events are sent to.<br/>
        /// E.g. <c>ws:[::1]:52089</c> for WebSockets, <c>udp:127.0.0.1:60005</c> for UDP or <c>in-process</c></summary>
        public              abstract string Endpoint { get; }
        public              abstract bool   IsOpen ();
        public              abstract bool   IsRemoteTarget ();
        /// <summary>Send a serialized <see cref="EventMessage"/> to the receiver</summary>
        protected internal  abstract void   SendEvent(in ClientEvent clientEvent);
    }
    
    public readonly struct ClientEvent
    {
        /// <summary>the <see cref="ProtocolEvent.dstClientId"/> of the <see cref="message"/></summary>
        public  readonly    ShortString dstClientId;
        /// <summary>serialized <see cref="EventMessage"/></summary>
        public  readonly    JsonValue   message;

        public  override    string      ToString() => $"client: {dstClientId}";

        public ClientEvent(in ShortString dstClientId, in JsonValue message) {
            this.dstClientId    = dstClientId;
            this.message        = message;
        }
    }
}