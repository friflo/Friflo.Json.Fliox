// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Remote.TransportUtils;

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    public sealed class UdpServerSync : IHost, IRemoteServer, ILogSource, IDisposable
    {
        internal readonly   FlioxHub                                    hub;
        private  readonly   int                                         recvCount;
        private             bool                                        running;
        private             Socket                                      socket;
        private  readonly   IPEndPoint                                  ipEndPoint;
        internal readonly   MessageBufferQueueSync<UdpMeta>             sendQueue;
        private  readonly   List<MessageItem<UdpMeta>>                  messages;
        private  readonly   RemoteHostEnv                               hostEnv;
        private  readonly   Dictionary<IPEndPoint, UdpSocketSyncHost>   clients;    // requires lock
        private             StringBuilder                               sbSend;
        private  readonly   IHubLogger                                  logger;
        public              IHubLogger                                  Logger => logger;
        
        public UdpServerSync(string endpoint, FlioxHub hub, int receiverCount = 1) {
            this.hub        = hub;
            recvCount       = receiverCount;
            ipEndPoint      = ParseEndpoint(endpoint) ?? throw new ArgumentException($"invalid endpoint: {endpoint}");
            logger          = hub.Logger;
            sendQueue       = new MessageBufferQueueSync<UdpMeta>();
            messages        = new List<MessageItem<UdpMeta>>();
            hostEnv         = hub.GetFeature<RemoteHostEnv>();
            clients         = new Dictionary<IPEndPoint, UdpSocketSyncHost>();
        }

        public void Dispose() {
            sendQueue.Close();    
        }
        
        // --- IRemoteServer
        public void     Start   () {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // socket.SendBufferSize    = 10 * 0x10000;
            socket.ReceiveBufferSize    = 10 * 0x10000; // if too small received messages get dropped if buffer is full
            socket.Bind(ipEndPoint);
        }
        public void     Run     () => SendReceiveMessages();
        public Task     RunAsync() => Task.Run(SendReceiveMessages);
        public void     Stop    () {
            running = false;
            UdpUtils.CloseSocket(socket);
            sendQueue.Close();
        }
        
        /// <summary>
        /// Loop is purely I/O bound => don't wrap in
        /// return Task.Run(async () => { ... });
        /// </summary>
        /// <remarks>
        /// A send loop reading from a queue is required as message can be sent from two different sources <br/>
        /// 1. response messages created in <see cref="Receiver.ReceiveMessageLoop"/> <br/>
        /// 2. event messages send with <see cref="SocketHost.SendEvent"/>'s <br/>
        /// The loop ensures a UdpClient.SendAsync() is called only once at a time.
        /// </remarks>
        private void RunSendMessageLoop() {
            try {
                SendMessageLoop();
            } catch (Exception e) {
                var msg = GetExceptionMessage("UdpServer.RunSendMessageLoop()", ipEndPoint, e);
                logger.Log(HubLog.Info, msg);
            }
        }
        
        /// Send queue is required to ensure having only a single outstanding SendAsync() at any time
        private void SendMessageLoop() {
            while (running) {
                var remoteEvent = sendQueue.DequeMessages(messages);
                
                // if (messages.Count >= 2) { Console.WriteLine("dequeued messages " + messages.Count); }
                foreach (var message in messages) {
                    if (hostEnv.logMessages) LogMessage(logger, ref sbSend, " server ->", message.meta.remoteEndPoint, message.value);
                    var msg  = message.value;
                    socket.SendTo(msg.MutableArray, msg.start, msg.Count, SocketFlags.None, message.meta.remoteEndPoint);
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        private void RunReceiveMessageLoop() {
            var receiver = new Receiver(this);
            receiver.ReceiveMessageLoop();
        }
        
        private class Receiver {
            private  readonly   UdpServerSync                               server;
            private  readonly   Socket                                      socket;
            private  readonly   RemoteHostEnv                               hostEnv;
            private  readonly   Dictionary<IPEndPoint, UdpSocketSyncHost>   clients;    // requires lock
            private  readonly   IHubLogger                                  logger;
            private             StringBuilder                               sbRecv;
            private readonly    IPEndPoint                                  endPointCache = IPEndPointCache.Create(IPAddress.Any, 0);
            
            internal Receiver(UdpServerSync server) {
                this.server = server;
                socket      = server.socket;
                hostEnv     = server.hostEnv;
                clients     = server.clients;
                logger      = server.logger;
            }
        
            /// <summary>
            /// Parse, execute and send response message for all received request messages.<br/>
            /// </summary>
            internal void ReceiveMessageLoop() {
                var buffer = new byte[0x10000];
                while (server.running) {
                    try {
                        // --- Read message from socket
                        EndPoint endpoint   = endPointCache;
                        int receivedBytes   = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);
                        
                        // --- Get remote host from IP address
                        var remoteEndpoint  = (IPEndPoint)endpoint;
                        UdpSocketSyncHost remote;
                        lock (clients) {
                            if (!clients.TryGetValue(remoteEndpoint, out remote)) {
                                remote                      = new UdpSocketSyncHost(server, remoteEndpoint);
                                clients[remote.endpoint]    = remote;
                            }                        
                        }
                        // --- Process message
                        var request = new JsonValue(buffer, receivedBytes);
                        if (hostEnv.logMessages) LogMessage(logger, ref sbRecv, " server <-", remote.endpoint, request);
                        remote.OnReceive(request, ref hostEnv.metrics.udp);
                    }
                    catch (SocketException socketException) {
                        if (UdpUtils.IsIgnorable(socketException))
                            continue;
                        if (UdpUtils.IsClosed(socketException)) {
                            logger.Log(HubLog.Info, $"UdpServerSync stopped: {socketException.Message}");
                            return;
                        }
                        var msg = GetExceptionMessage("UdpServerSync stopped", server.ipEndPoint, socketException);
                        logger.Log(HubLog.Error, msg);
                        return;
                    }
                    catch (Exception exception) {
                        var msg = GetExceptionMessage("UdpServerSync stopped", server.ipEndPoint, exception);
                        logger.Log(HubLog.Error, msg);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="FlioxHub.Logger"/> <br/>
        /// </summary>
        private void SendReceiveMessages()
        {
            if (socket == null) {
                var error = $"UdpServerSync requires Start() before Run() endpoint: {ipEndPoint}";
                Logger.Log(HubLog.Error, error);
                throw new InvalidOperationException(error);
            }
            running         = true;
            var startMsg = $"UdpServerSync listening at: {ipEndPoint}";
            Logger.Log(HubLog.Info, startMsg);
            Thread sendLoop = null;
            try {
                sendLoop        = new Thread(RunSendMessageLoop)    { Name = $"UDP:{ipEndPoint.Port} send" };
                var recvThreads = new List<Thread>();
                for (int n = 0; n < recvCount; n++) {
                    var thread  = new Thread(RunReceiveMessageLoop) { Name = $"UDP:{ipEndPoint.Port} recv-{n}" };
                    recvThreads.Add(thread);
                }
                sendLoop.Start();
                foreach (var thread in recvThreads) { thread.Start(); }
                foreach (var thread in recvThreads) { thread.Join();  }
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