// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{

    /// <summary>
    /// Implementation aligned with <see cref="WebSocketHost"/>
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="UdpSocketClientHub"/> used by the server.<br/>
    /// </remarks>
    internal sealed class UdpSocketHost : SocketHost
    {
        internal readonly   IPEndPoint  remoteClient; // client
        private  readonly   UdpServer   server;

        internal UdpSocketHost (
            UdpServer   server,
            IPEndPoint  remoteClient)
        : base (server.hub, server.hostEnv)
        {
            this.server         = server;
            this.remoteClient = remoteClient;
        }
        
        // --- IEventReceiver
        public override bool    IsRemoteTarget ()   => true;
        public override bool    IsOpen ()           => true;
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message, in SocketContext socketContext) {
            server.sendQueue.AddTail(message, new UdpMeta(remoteClient));
        }
        
        internal void OnReceive(in JsonValue request)
        {
            var socketContext   = new SocketContext(remoteClient);
            var hostMetrics     = server.hostMetrics;
            // --- precondition: message was read from socket
            try {
                // --- 1. Parse request
                Interlocked.Increment(ref hostMetrics.udp.receivedCount);
                var t1          = Stopwatch.GetTimestamp();
                var syncRequest = ParseRequest(request, socketContext);
                var t2          = Stopwatch.GetTimestamp();
                Interlocked.Add(ref hostMetrics.udp.requestReadTime, t2 - t1);
                if (syncRequest == null) {
                    return;
                }
                // --- 2. Execute request
                ExecuteRequest (syncRequest, socketContext);
                var t3          = Stopwatch.GetTimestamp();
                Interlocked.Add(ref hostMetrics.udp.requestExecuteTime, t3 - t2);
            }
            catch (Exception e) {
                SendResponseException(e, null, socketContext);
            }
        }
    }
}