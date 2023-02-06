// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Implementation aligned with <see cref="WebSocketHost"/>
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="UdpRefSocketClientHub"/> used by the server.<br/>
    /// </remarks>
    internal sealed class UdpRefSocketHost : SocketHost
    {
        internal readonly   IPEndPoint      remoteClient;
        private  readonly   UdpRefServer    server;

        internal UdpRefSocketHost (UdpRefServer server, IPEndPoint  remoteClient)
        : base (server.hub)
        {
            this.server         = server;
            this.remoteClient   = remoteClient;
        }
        
        // --- IEventReceiver
        protected internal override bool    IsRemoteTarget ()   => true;
        protected internal override bool    IsOpen ()           => true;
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message) {
            server.sendQueue.AddTail(message, new UdpMeta(remoteClient));
        }
    }
}