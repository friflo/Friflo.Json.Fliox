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
        internal  readonly  UdpClient           client;
        internal  readonly  RemoteRequestMap    requestMap;

        /// <summary>if port == 0 an available port is used</summary>
        internal UdpSocket(int port) {
            client              = new UdpClient();
            var localEndPoint   = new IPEndPoint(IPAddress.Any, port);
            client.Client.Bind(localEndPoint);
            requestMap          = new RemoteRequestMap();
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
        private  readonly   UdpSocket                   socket;
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
            socket      = new UdpSocket(port);
            localPort   = ((IPEndPoint)socket.client.Client.LocalEndPoint).Port;
            // TODO check if running loop from here is OK
            var _ = RunReceiveMessageLoop(socket);
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
        private async Task ReceiveMessageLoop(UdpSocket socket, ObjectReader reader) {
            var client          = socket.client;
            while (true)
            {
                try {
                    // --- read complete datagram message
                    var receiveResult   = await client.ReceiveAsync().ConfigureAwait(false);
                    var buffer          = receiveResult.Buffer;

                    // --- process received message
                    var message = new JsonValue(buffer, buffer.Length);
                    if (logMessages) {
                        Logger.Log(HubLog.Info, $"c:{localPort,5} <-{remoteHost,20} {message.AsString().Truncate()}");
                    }
                    ProcessMessage(message, socket.requestMap, reader);
                }
                catch (Exception e)
                {
                    var message = $"WebSocketClientHub receive error: {e.Message}";
                    Logger.Log(HubLog.Error, message, e);
                    socket.requestMap.CancelRequests();
                }
            }
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            int sendReqId       = Interlocked.Increment(ref reqId);
            syncRequest.reqId   = sendReqId;
            try {
                // requires its own mapper - method can be called from multiple threads simultaneously
                using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                    var writer              = pooledMapper.instance.writer;
                    writer.Pretty           = false;
                    writer.WriteNullMembers = false;
                    var rawRequest  = RemoteMessageUtils.CreateProtocolMessage(syncRequest, writer);
                    // request need to be queued _before_ sending it to be prepared for handling the response.
                    var request     = new RemoteRequest(syncContext, cancellationToken);
                    socket.requestMap.Add(sendReqId, request);
                    if (logMessages) {
                        Logger.Log(HubLog.Info, $"c:{localPort,5} ->{remoteHost,20} {rawRequest.AsString().Truncate()}");
                    }
                    // --- Send message
                    await socket.client.SendAsync(rawRequest.MutableArray, rawRequest.Count, remoteHost).ConfigureAwait(false);
                    
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
