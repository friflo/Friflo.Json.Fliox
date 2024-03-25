// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{

    /// <summary>
    /// A <see cref="FlioxHub"/> accessed remotely  using a <see cref="UdpClient"/> connection<br/>
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="UdpServerSync"/> used by clients.<br/>
    /// Implementation aligned with <see cref="WebSocketClientHub"/>
    /// </remarks>
    public sealed class UdpSocketSyncClientHub : SocketClientHub
    {
        private  readonly   IPEndPoint                  remoteHost;
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public   override   bool                        IsConnected => true;
        private  readonly   UdpSocket                   udp;
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        private  readonly   int                         localPort;
        
        public   override   string                      ToString() => $"{database.name} - port: {localPort}";
        
        /// <summary>
        /// if port == 0 an available port is used
        /// </summary>
        public UdpSocketSyncClientHub(string dbName, string remoteHost, int port = 0, SharedEnv env = null, RemoteClientAccess access = RemoteClientAccess.Multi)
            : base(new RemoteDatabase(dbName), env, ProtocolFeature.Duplicates, access)
        {
            var ipEndPoint  = TransportUtils.ParseEndpoint(remoteHost) ?? throw new ArgumentException($"invalid remoteHost: {remoteHost}");
            this.remoteHost = IPEndPointReuse.Create(ipEndPoint.Address, ipEndPoint.Port);
            udp             = new UdpSocket(port);
            localPort       = udp.GetPort();
            // Connect() enable using Socket.Receive() & Send() instead of ReceiveFrom() & SendTo()
            udp.socket.Connect(this.remoteHost);
            // TODO check if running loop from here is OK
            var thread  = new Thread(ReceiveMessageLoop) { Name = $"client:{localPort} UDP recv" };
            thread.Start();
        }
        
        public override Task Close() {
            // socket.Close()
            // - unbind the local port
            // - throw SocketException with SocketError.Interrupted or ObjectDisposedException in Socket.Receive()
            UdpUtils.CloseSocket(udp.socket);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Has no SendMessageLoop() - client send only response messages via <see cref="SocketClientHub.OnReceive"/>
        /// </summary>
        private void ReceiveMessageLoop() {
            using var   mapper = new ObjectMapper(sharedEnv.typeStore);
            var         reader = mapper.reader;
            var buffer = new byte[0x10000];
            while (true)
            {
                try {
                    // --- read complete datagram message
                    var receivedBytes   = udp.socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                        
                    var message         = new JsonValue(buffer, receivedBytes);

                    // --- process received message
                    if (env.logMessages) TransportUtils.LogMessage(Logger, ref sbRecv, $"c:{localPort,5} <-", remoteHost, message);
                    OnReceive(message, udp.requestMap, reader);
                }
                catch (SocketException e) {
                    if (e.SocketErrorCode != SocketError.Interrupted) {
                        Logger.Log(HubLog.Info, $"UdpSocketSyncClientHub receive error: {e.Message}");
                    }
                    udp.requestMap.CancelRequests();
                    return;
                }
                catch (ObjectDisposedException) {
                    udp.requestMap.CancelRequests();
                    return;
                }
                catch (Exception e) {
                    Logger.Log(HubLog.Error, $"UdpSocketSyncClientHub receive error: {e.Message}", e);
                    udp.requestMap.CancelRequests();
                    return;
                }
            }
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            int sendReqId       = Interlocked.Increment(ref reqId);
            syncRequest.reqId   = sendReqId;
            try {
                // requires its own mapper - method can be called from multiple threads simultaneously
                using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                    var writer      = MessageUtils.GetCompactWriter(pooledMapper.instance);
                    var rawRequest  = MessageUtils.WriteProtocolMessage(syncRequest, sharedEnv, writer);
                    // request need to be queued _before_ sending it to be prepared for handling the response.
                    var request     = new RemoteRequest(syncContext, cancellationToken);
                    udp.requestMap.Add(sendReqId, request);
                    
                    if (env.logMessages) TransportUtils.LogMessage(Logger, ref sbSend, $"c:{localPort,5} ->", remoteHost, rawRequest);
                    // --- Send message
                    udp.socket.Send(rawRequest.MutableArray, rawRequest.start, rawRequest.Count, SocketFlags.None);
                    
                    // --- Wait for response
                    var response = await request.response.Task.ConfigureAwait(false);
                    
                    return CreateSyncResult(response);
                }
            }
            catch (Exception e) {
                return CreateSyncError(e, remoteHost);
            }
        }
    }
}
