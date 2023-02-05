// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Utils;


namespace Friflo.Json.Fliox.Hub.Remote.Transport.UdpRef
{
    public sealed class UdpServer : IDisposable, ILogSource
    {
        internal readonly   FlioxHub                                hub;
        private  readonly   UdpClient                               udpClient;
        private  readonly   IPEndPoint                              ipEndPoint;
        internal readonly   HostEnv                                 hostEnv = new HostEnv();
        internal readonly   MessageBufferQueueAsync<UdpMeta>        sendQueue;
        private  readonly   List<MessageItem<UdpMeta>>              messages;
        private  readonly   HostMetrics                             hostMetrics;
        private  readonly   Dictionary<IPEndPoint, UdpSocketHost>   clients;
        private  readonly   bool                                    logMessages = false;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger  Logger { get; }
        
        public UdpServer(string endpoint, FlioxHub hub) {
            this.hub    = hub;
            ipEndPoint  = TransportUtils.ParseEndpoint(endpoint) ?? throw new ArgumentException($"invalid endpoint: {endpoint}");
            udpClient   = new UdpClient(ipEndPoint);
            Logger      = hub.Logger;
            sendQueue   = new MessageBufferQueueAsync<UdpMeta>();
            messages    = new List<MessageItem<UdpMeta>>();
            hostMetrics = hostEnv.metrics;
            clients     = new Dictionary<IPEndPoint, UdpSocketHost>();
        }

        public void Dispose() {
            sendQueue.Dispose();
        }

        public async Task Run() {
            await SendReceiveMessages().ConfigureAwait(false);
        }
        
        /// <summary>
        /// Loop is purely I/O bound => don't wrap in
        /// return Task.Run(async () => { ... });
        /// </summary>
        /// <remarks>
        /// A send loop reading from a queue is required as message can be sent from two different sources <br/>
        /// 1. response messages created in <see cref="ReceiveMessageLoop"/> <br/>
        /// 2. event messages send with <see cref="SocketHost.SendEvent"/>'s <br/>
        /// The loop ensures a UdpClient.SendAsync() is called only once at a time.
        /// </remarks>
        private async Task RunSendMessageLoop() {
            try {
                await SendMessageLoop().ConfigureAwait(false);
            } catch (Exception e) {
                var msg = TransportUtils.GetExceptionMessage("UdpServer.RunSendMessageLoop()", ipEndPoint, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        // Send queue (sendWriter / sendReader) is required  to prevent having more than one UdpClient.SendAsync() call outstanding.
        private async Task SendMessageLoop() {
            var buffer = new byte[128]; // UdpClient.SendAsync() requires datagram array starting datagram[0]
            while (true) {
                var remoteEvent = await sendQueue.DequeMessagesAsync(messages).ConfigureAwait(false);
                foreach (var message in messages) {
                    if (logMessages) TransportUtils.LogMessage(Logger, " server ->", message.meta.remoteEndPoint, message.value);
                    message.value.CopyTo(ref buffer);
                    // ReSharper disable once PossibleNullReferenceException
                    await udpClient.SendAsync(buffer, message.value.Count, message.meta.remoteEndPoint).ConfigureAwait(false);
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        private async Task RunReceiveMessageLoop() {
            await ReceiveMessageLoop().ConfigureAwait(false);
        }
        
        private async Task ReceiveMessageLoop() {
            while (true) {
                // --- 1. Read request from datagram
                var receiveResult = await udpClient.ReceiveAsync().ConfigureAwait(false);
                
                var remoteEndpoint = receiveResult.RemoteEndPoint;
                if (!clients.TryGetValue(remoteEndpoint, out var socketHost)) {
                    socketHost              = new UdpSocketHost(this, remoteEndpoint);
                    clients[remoteEndpoint] = socketHost;
                }
                var buffer  = receiveResult.Buffer;
                var request = new JsonValue(buffer, buffer.Length);
                if (logMessages) TransportUtils.LogMessage(Logger, " server <-", socketHost.remoteClient, request);
                socketHost.OnReceive(request, hostMetrics.udp);
            }
        }
        
        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="FlioxHub.Logger"/> <br/>
        /// </summary>
        private async Task SendReceiveMessages()
        {
            Task sendLoop   = null;
            try {
                sendLoop = RunSendMessageLoop();

                await RunReceiveMessageLoop().ConfigureAwait(false);

                sendQueue.Close();
            }
            catch (Exception e) {
                var msg = TransportUtils.GetExceptionMessage("UdpServer.SendReceiveMessages()", ipEndPoint, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            finally {
                if (sendLoop != null) {
                    await sendLoop.ConfigureAwait(false);
                }
                udpClient?.Dispose();
            }
        }
    }
}