// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Implementation aligned with <see cref="WebSocketHost"/>
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="UdpSocketClientHub"/> used by the server.<br/>
    /// </remarks>
    internal sealed class UdpSocketSyncHost : SocketHost
    {
        internal readonly   IPEndPoint      endpoint;
        private  readonly   UdpServerSync   server;

        internal UdpSocketSyncHost (UdpServerSync server, IPEndPoint endpoint)
        : base (server.hub, server)
        {
            this.server     = server;
            this.endpoint   = IPEndPointReuse.Create(endpoint.Address, endpoint.Port);
        }
        
        // --- IEventReceiver
        public override string  Endpoint            => $"udp:{endpoint}";
        public override bool    IsRemoteTarget ()   => true;
        public override bool    IsOpen ()           => true;
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message) {
            var queue = server.sendQueue;
            if (queue.Closed)
                return;
            queue.AddTail(message, new UdpMeta(endpoint));
        }
    }
}