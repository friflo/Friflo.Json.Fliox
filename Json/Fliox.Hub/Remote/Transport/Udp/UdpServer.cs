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
        private  readonly   Socket                                  socket;
        private  readonly   UdpClient                               udpClient;
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
            socket      = new Socket(SocketType.Dgram, ProtocolType.Udp);
            if (socket != null) {
                socket.Bind(ipEndPoint);
            } else {
                udpClient = new UdpClient(ipEndPoint); // reference implementation using UdpClient 
            }
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
            var buffer = udpClient != null ? new byte[128] : null; // UdpClient.SendAsync() requires datagram array starting datagram[0]
            while (true) {
                var remoteEvent = await sendQueue.DequeMessagesAsync(messages).ConfigureAwait(false);
                foreach (var message in messages) {
                    if (logMessages) {
                        Logger.Log(HubLog.Info, $" server ->{message.meta.remoteEndPoint,20} {message.value.AsString().Truncate()}");
                    }
                    if (socket != null) {
                        var array = message.value.AsMutableArraySegment();
                        await socket.SendToAsync(array, SocketFlags.None, message.meta.remoteEndPoint).ConfigureAwait(false);
                    } else {
                        message.value.CopyTo(ref buffer);
                        // ReSharper disable once PossibleNullReferenceException
                        await udpClient.SendAsync(buffer, message.value.Count, message.meta.remoteEndPoint).ConfigureAwait(false);
                    }
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        private async Task RunReceiveMessageLoop() {
            if (socket != null) {
                await SocketReceiveMessageLoop().ConfigureAwait(false);
            } else {
                await ReceiveMessageLoop().ConfigureAwait(false);
            }
        }
        
        private static readonly IPEndPoint DummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
        
        /// <summary>
        /// Parse, execute and send response message for all received request messages.<br/>
        /// </summary>
        private async Task SocketReceiveMessageLoop() {
            var buffer          = new byte[0x10000];
            var bufferSegment   = new ArraySegment<byte>(buffer);
            while (true) {
                // --- 1. Read request from datagram
                var result = await socket.ReceiveFromAsync(bufferSegment, SocketFlags.None, DummyEndpoint);
                
                var remoteEndpoint  = (IPEndPoint)result.RemoteEndPoint;
                if (!clients.TryGetValue(remoteEndpoint, out var socketHost)) {
                    socketHost              = new UdpSocketHost(this, remoteEndpoint);
                    clients[remoteEndpoint] = socketHost;
                }
                var request = new JsonValue(buffer, result.ReceivedBytes);
                if (logMessages) {
                    Logger.Log(HubLog.Info, $" server <-{socketHost.remoteClient,20} {request.AsString().Truncate()}");
                }
                socketHost.OnReceive(request, hostMetrics.udp);
            }
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
                if (logMessages) {
                    Logger.Log(HubLog.Info, $" server <-{socketHost.remoteClient,20} {request.AsString().Truncate()}");
                }
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
                socket?.Dispose();
                udpClient?.Dispose();
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