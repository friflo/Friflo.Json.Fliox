// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Remote.TransportUtils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    public sealed class UdpServerSync : IServer, IDisposable, ILogSource
    {
        internal readonly   FlioxHub                                    hub;
        private             bool                                        running;
        private             Socket                                      socket;
        private  readonly   IPEndPoint                                  ipEndPoint;
        internal readonly   MessageBufferQueueSync<UdpMeta>             sendQueue;
        private  readonly   List<MessageItem<UdpMeta>>                  messages;
        private  readonly   RemoteHostEnv                               hostEnv;
        private  readonly   Dictionary<IPEndPoint, UdpSocketSyncHost>   clients;
        private             StringBuilder                               sbSend;
        private             StringBuilder                               sbRecv;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger  Logger { get; }
        
        public UdpServerSync(string endpoint, FlioxHub hub) {
            this.hub        = hub;
            ipEndPoint      = ParseEndpoint(endpoint) ?? throw new ArgumentException($"invalid endpoint: {endpoint}");
            Logger          = hub.Logger;
            sendQueue       = new MessageBufferQueueSync<UdpMeta>();
            messages        = new List<MessageItem<UdpMeta>>();
            hostEnv         = hub.GetFeature<RemoteHostEnv>();
            clients         = new Dictionary<IPEndPoint, UdpSocketSyncHost>();
        }

        public void Dispose() {
            sendQueue.Close();    
        }
        
        // --- IServer
        public void     Start   () {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(ipEndPoint);
        }
        public void     Run     () => SendReceiveMessages();
        public Task     RunAsync() => Task.Run(SendReceiveMessages);
        public void     Stop    () {
            running = false;
            socket.Close();
            sendQueue.Close();
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
        private void RunSendMessageLoop() {
            try {
                SendMessageLoop();
            } catch (Exception e) {
                var msg = GetExceptionMessage("UdpServer.RunSendMessageLoop()", ipEndPoint, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        /// Send queue is required to ensure having only a single outstanding SendAsync() at any time
        private void SendMessageLoop() {
            while (running) {
                var remoteEvent = sendQueue.DequeMessages(messages);
                
                // if (messages.Count >= 2) { Console.WriteLine("dequeued messages " + messages.Count); }
                foreach (var message in messages) {
                    if (hostEnv.logMessages) LogMessage(Logger, ref sbSend, " server ->", message.meta.remoteEndPoint, message.value);
                    var msg  = message.value;
                    var send = socket.SendTo(msg.MutableArray, msg.start, msg.Count, SocketFlags.None, message.meta.remoteEndPoint);
                    
                    if (send != msg.Count) throw new InvalidOperationException($"UdpServerSync - send error. expected: {msg.Count}, was: {send}");
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        private void RunReceiveMessageLoop() {
            try {
                ReceiveMessageLoop();
            } catch (Exception e){
                var msg = GetExceptionMessage("UdpServerSync.RunReceiveMessageLoop()", ipEndPoint, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        private readonly IPEndPointCache endPointCache = new IPEndPointCache(IPAddress.Any, 0);
        
        /// <summary>
        /// Parse, execute and send response message for all received request messages.<br/>
        /// </summary>
        private void ReceiveMessageLoop() {
            var buffer = new byte[0x10000];
            while (running) {
                // --- 1. Read request from datagram
                EndPoint endpoint   = endPointCache;
                int receivedBytes   = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);
                
                var remoteEndpoint  = (IPEndPoint)endpoint;
                if (!clients.TryGetValue(remoteEndpoint, out var remote)) {
                    remote                      = new UdpSocketSyncHost(this, remoteEndpoint);
                    clients[remote.endpoint]    = remote;
                }
                var request = new JsonValue(buffer, receivedBytes);
                if (hostEnv.logMessages) LogMessage(Logger, ref sbRecv, " server <-", remote.endpoint, request);
                remote.OnReceive(request, ref hostEnv.metrics.udp);
            }
        }

        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="FlioxHub.Logger"/> <br/>
        /// </summary>
        private void SendReceiveMessages()
        {
            if (socket == null) throw new InvalidOperationException("server not started");
            running         = true;
            Thread sendLoop = null;
            try {
                sendLoop        = new Thread(RunSendMessageLoop)    { Name = $"UDP:{ipEndPoint.Port} send" };
                var recvLoop    = new Thread(RunReceiveMessageLoop) { Name = $"UDP:{ipEndPoint.Port} recv" };
                sendLoop.Start();
                recvLoop.Start();
                recvLoop.Join();
                sendQueue.Close();
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("UdpServer.SendReceiveMessages()", ipEndPoint, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            finally {
                if (sendLoop != null) {
                    sendLoop.Join();
                }
                socket?.Dispose();
            }
        }
    }
}