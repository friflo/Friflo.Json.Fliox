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

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public sealed class UdpServer : IDisposable, ILogSource
    {
        internal readonly   FlioxHub                                hub;
        private  readonly   UdpClient                               udpListener;
        private  readonly   IPEndPoint                              ipEndPoint;
        internal readonly   HostEnv                                 hostEnv = new HostEnv();
        internal readonly   MessageBufferQueueAsync<UdpMeta>        sendQueue;
        private  readonly   List<MessageItem<UdpMeta>>              messages;
        internal readonly   HostMetrics                             hostMetrics;
        private  readonly   Dictionary<IPEndPoint, UdpSocketHost>   clients;
        private  readonly   bool                                    logMessages = false;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger  Logger { get; }
        
        public UdpServer(string endpoint, FlioxHub hub) {
            if (!TransportUtils.TryParseEndpoint(endpoint, out ipEndPoint)) {
                throw new ArgumentException($"invalid endpoint: {endpoint}", nameof(endpoint));
            }
            this.hub    = hub;
            udpListener = new UdpClient(ipEndPoint);
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
                var msg = GetExceptionMessage("RunSendMessageLoop()", ipEndPoint, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        // Send queue (sendWriter / sendReader) is required  to prevent having more than one UdpClient.SendAsync() call outstanding.
        private async Task SendMessageLoop() {
            var buffer = new byte[128]; // required: UdpListener requires datagram array starting datagram[0]
            while (true) {
                var remoteEvent = await sendQueue.DequeMessagesAsync(messages).ConfigureAwait(false);
                foreach (var message in messages) {
                    message.value.CopyTo(ref buffer);
                    if (logMessages) {
                        Logger.Log(HubLog.Info, $" server ->{message.meta.remoteEndPoint,20} {message.value.AsString().Truncate()}");
                    }
                    await udpListener.SendAsync(buffer, message.value.Count, message.meta.remoteEndPoint).ConfigureAwait(false);
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        private async Task RunReceiveMessageLoop() {
            await ReceiveMessageLoop().ConfigureAwait(false);
        }
        
        /// <summary>
        /// Parse, execute and send response message for all received request messages.<br/>
        /// </summary>
        private async Task ReceiveMessageLoop() {
            while (true) {
                // --- 1. Read request from datagram
                var receiveResult   = await udpListener.ReceiveAsync().ConfigureAwait(false);
                var remoteEndpoint  = receiveResult.RemoteEndPoint;
                if (!clients.TryGetValue(remoteEndpoint, out var socketHost)) {
                    socketHost              = new UdpSocketHost(this, remoteEndpoint);
                    clients[remoteEndpoint] = socketHost;
                }
                var buffer  = receiveResult.Buffer;
                var request = new JsonValue(buffer, buffer.Length);
                if (logMessages) {
                    Logger.Log(HubLog.Info, $" server <-{socketHost.remoteClient,20} {request.AsString().Truncate()}");
                }
                socketHost.OnReceive(request);
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
            catch (SocketException e) {
                var msg = GetExceptionMessage("UdpSocketHost.SendReceiveMessages()", ipEndPoint, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("UdpSocketHost.SendReceiveMessages()", ipEndPoint, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            finally {
                if (sendLoop != null) {
                    await sendLoop.ConfigureAwait(false);
                }
                udpListener.Dispose();
            }
        }
        
        private static string GetExceptionMessage(string location, IPEndPoint remoteEndPoint, Exception e) {
            if (e is SocketException wsException) {
                return $"{location} {e.GetType().Name} {e.Message} ErrorCode: {wsException.ErrorCode}, HResult: 0x{e.HResult:X}, remote: {remoteEndPoint}";
            }
            return $"{location} {e.GetType().Name}: {e.Message}, remote: {remoteEndPoint}";
        }
    }
    
    internal readonly struct UdpMeta
    {
        internal readonly   IPEndPoint  remoteEndPoint;

        public   override   string      ToString() => remoteEndPoint.ToString();

        internal UdpMeta (IPEndPoint remoteEndPoint) {
            this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        }
    }
}