// Copyright (c) Ullrich Praetz. All rights reserved.
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
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// Each <see cref="WebSocketConnection"/> store its send requests in the <see cref="requestMap"/>
    /// to map received response messages to its related <see cref="SyncRequest"/>
    /// </summary>
    internal sealed class UdpSocket
    {
        internal  readonly  Socket              socket;
        internal  readonly  UdpClient           udpClient;
        internal  readonly  RemoteRequestMap    requestMap;

        /// <summary>if port == 0 an available port is used</summary>
        internal UdpSocket(int port) {
            var localEndPoint   = new IPEndPoint(IPAddress.Any, port);
            // setting AddressFamily.InterNetwork improves performance - did not analyzed reason
            socket              = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (socket != null) {
                socket.Bind(localEndPoint);
            } else {
                udpClient  = new UdpClient();
                udpClient.Client.Bind(localEndPoint);
            }
            requestMap = new RemoteRequestMap();
        }

        internal int GetPort() {
            if (socket != null) {
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
            return ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        }
    }


    /// <summary>
    /// A <see cref="FlioxHub"/> accessed remotely  using a <see cref="UdpClient"/> connection<br/>
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="UdpSocketHost"/> used by clients.<br/>
    /// Implementation aligned with <see cref="WebSocketClientHub"/>
    /// </remarks>
    public sealed class UdpSocketClientHub : SocketClientHub
    {
        private  readonly   IPEndPoint                  remoteHost;
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public              bool                        IsConnected => true;
        private  readonly   UdpSocket                   udpSocket;
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        private  readonly   bool                        logMessages = false;
        private  readonly   int                         localPort;
        
        public   override   string                      ToString() => $"{database.name} - port: {localPort}";
        
        /// <summary>
        /// if port == 0 an available port is used
        /// </summary>
        public UdpSocketClientHub(string dbName, string remoteHost, int port = 0, SharedEnv env = null, RemoteClientAccess access = RemoteClientAccess.Multi)
            : base(new RemoteDatabase(dbName), env, access)
        {
            TransportUtils.TryParseEndpoint(remoteHost, out this.remoteHost);
            udpSocket   = new UdpSocket(port);
            localPort   = udpSocket.GetPort();
            // TODO check if running loop from here is OK
            var _ = RunReceiveMessageLoop(udpSocket);
        }
        
        /* public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        } */
        
        private async Task RunReceiveMessageLoop(UdpSocket socket) {
            using (var mapper = new ObjectMapper(sharedEnv.TypeStore)) {
                await ReceiveMessageLoop(socket, mapper.reader).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// In contrast to <see cref="WebSocketHost"/> the <see cref="WebSocketClientHub"/> has no SendMessageLoop() <br/>
        /// This is possible because WebSocket messages are only response messages created in this loop. <br/>
        /// As <see cref="ReceiveMessageLoop"/> is called sequentially in the loop, WebSocket.SendAsync() is called only once at any time.
        /// Infos: <br/>
        /// - A blocking WebSocket.SendAsync() call does not block WebSocket.ReceiveAsync() <br/>
        /// - The created <see cref="RemoteRequest.response"/>'s act as a queue. <br/>
        /// </summary>
        private async Task ReceiveMessageLoop(UdpSocket udpSocket, ObjectReader reader) {
            var bufferSegment = udpSocket.socket != null ? new ArraySegment<byte>(new byte[0x10000]) : default;
            while (true)
            {
                try {
                    // --- read complete datagram message
                    JsonValue message;
                    if (udpSocket.socket != null) {
                        var result  = await udpSocket.socket.ReceiveFromAsync(bufferSegment, SocketFlags.None, remoteHost).ConfigureAwait(false);
                        message     = new JsonValue(bufferSegment.Array, result.ReceivedBytes);
                        // note: using ReceiveFromAsync() is faster than ReceiveAsync() - did not analyzed reason
                        // int length  = await udpSocket.socket.ReceiveAsync(bufferSegment, SocketFlags.None).ConfigureAwait(false);
                        // message     = new JsonValue(bufferSegment.Array, length);
                    } else {
                        var result  = await udpSocket.udpClient.ReceiveAsync().ConfigureAwait(false);
                        var buffer  = result.Buffer;
                        message     = new JsonValue(buffer, buffer.Length);
                    }
                    // --- process received message
                    if (logMessages) {
                        Logger.Log(HubLog.Info, $"c:{localPort,5} <-{remoteHost,20} {message.AsString().Truncate()}");
                    }
                    OnReceive(message, udpSocket.requestMap, reader);
                }
                catch (Exception e)
                {
                    var message = $"WebSocketClientHub receive error: {e.Message}";
                    Logger.Log(HubLog.Error, message, e);
                    udpSocket.requestMap.CancelRequests();
                }
            }
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            int sendReqId       = Interlocked.Increment(ref reqId);
            syncRequest.reqId   = sendReqId;
            try {
                // requires its own mapper - method can be called from multiple threads simultaneously
                using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                    var writer      = RemoteMessageUtils.GetCompactWriter(pooledMapper.instance);
                    var rawRequest  = RemoteMessageUtils.CreateProtocolMessage(syncRequest, writer);
                    // request need to be queued _before_ sending it to be prepared for handling the response.
                    var request     = new RemoteRequest(syncContext, cancellationToken);
                    udpSocket.requestMap.Add(sendReqId, request);
                    if (logMessages) {
                        Logger.Log(HubLog.Info, $"c:{localPort,5} ->{remoteHost,20} {rawRequest.AsString().Truncate()}");
                    }
                    // --- Send message
                    if (udpSocket.socket != null) {
                        await udpSocket.socket.SendToAsync(rawRequest.AsMutableArraySegment(), SocketFlags.None, remoteHost).ConfigureAwait(false);
                    } else {
                        await udpSocket.udpClient.SendAsync(rawRequest.MutableArray, rawRequest.Count, remoteHost).ConfigureAwait(false);
                    }
                    // --- Wait for response
                    var response = await request.response.Task.ConfigureAwait(false);
                    
                    return CreateSyncResult(response);
                }
            }
            catch (Exception e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(remoteHost);
                var msg = error.ToString();
                return new ExecuteSyncResult(msg, ErrorResponseType.Exception);
            }
        }
    }
}
