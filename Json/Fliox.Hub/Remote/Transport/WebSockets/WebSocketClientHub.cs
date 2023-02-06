// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
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
    /// Store send requests in the <see cref="requestMap"/> to map received response messages to its related <see cref="SyncRequest"/>
    /// </summary>
    internal sealed class WebSocketConnection
    {
        internal  readonly  ClientWebSocket     websocket;
        internal  readonly  RemoteRequestMap    requestMap;
        
        internal WebSocketConnection() {
            websocket   = new ClientWebSocket();
            requestMap  = new RemoteRequestMap();
        }
    }


    /// <summary>
    /// A <see cref="FlioxHub"/> accessed remotely  using a <see cref="WebSocket"/> connection
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="WebSocketHost"/> used by clients.<br/>
    /// Implementation aligned with <see cref="Transport.Udp.UdpSocketClientHub"/>
    /// </remarks>
    public sealed partial class WebSocketClientHub : SocketClientHub
    {
        private  readonly   Uri                         remoteHost;
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public              bool                        IsConnected => wsConnection?.websocket.State == WebSocketState.Open;

        /// lock (<see cref="websocketLock"/>) {
        private readonly    object                      websocketLock = new object();
        private             WebSocketConnection         wsConnection;
        private             Task<WebSocketConnection>   connectTask;
        // }
        
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        
        public   override   string                      ToString() => $"{database.name} - endpoint: {remoteHost}";
        
        /// <summary>
        /// Create a remote <see cref="FlioxHub"/> by using a <see cref="WebSocket"/> connection
        /// </summary>
        public WebSocketClientHub(string dbName, string remoteHost, SharedEnv env = null, RemoteClientAccess access = RemoteClientAccess.Multi)
            : base(new RemoteDatabase(dbName), env, access)
        {
            this.remoteHost = new Uri(remoteHost);
        }
        
        /* public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        } */
        
        private async Task RunReceiveMessageLoop(WebSocketConnection socket) {
            using (var mapper = new ObjectMapper(sharedEnv.TypeStore)) {
                await ReceiveMessageLoop(socket, mapper.reader).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Has no SendMessageLoop() - client send only response messages via <see cref="SocketClientHub.OnReceive"/>
        /// </summary>
        private async Task ReceiveMessageLoop(WebSocketConnection socket, ObjectReader reader) {
            var buffer          = new ArraySegment<byte>(new byte[8192]);
            var ws              = socket.websocket;
            var memoryStream    = new MemoryStream();
            while (true)
            {
                memoryStream.Position = 0;
                memoryStream.SetLength(0);
                try {
                    // --- read complete WebSocket message
                    WebSocketReceiveResult wsResult;
                    do {
                        if (ws.State != WebSocketState.Open) {
                            // Logger.Log(HubLog.Info, $"Pre-ReceiveAsync. State: {ws.State}");
                            return;
                        }
                        wsResult = await ws.ReceiveAsync(buffer, cancellationToken.Token).ConfigureAwait(false);
                        memoryStream.Write(buffer.Array, buffer.Offset, wsResult.Count);
                    }
                    while(!wsResult.EndOfMessage);

                    if (ws.State != WebSocketState.Open) {
                        // Logger.Log(HubLog.Info, $"Post-ReceiveAsync. State: {ws.State}");
                        return;
                    }
                    if (wsResult.MessageType != WebSocketMessageType.Text) {
                        Logger.Log(HubLog.Error, $"Expect WebSocket message type text. type: {wsResult.MessageType} {remoteHost}");
                        continue;
                    }
                    var message     = new JsonValue(memoryStream.GetBuffer(), (int)memoryStream.Position);

                    // --- process received message
                    if (env.logMessages) TransportUtils.LogMessage(Logger, $"client  <-", remoteHost, message);
                    OnReceive(message, socket.requestMap, reader);
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
            var socket = GetWebsocketConnection();
            if (socket == null) {
                socket = await Connect().ConfigureAwait(false);
            }
            int sendReqId       = Interlocked.Increment(ref reqId);
            syncRequest.reqId   = sendReqId;
            try {
                // requires its own mapper - method can be called from multiple threads simultaneously
                using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                    var writer      = RemoteMessageUtils.GetCompactWriter(pooledMapper.instance);
                    var rawRequest  = RemoteMessageUtils.CreateProtocolMessage(syncRequest, writer);
                    // request need to be queued _before_ sending it to be prepared for handling the response.
                    var request     = new RemoteRequest(syncContext, cancellationToken);
                    socket.requestMap.Add(sendReqId, request);
                    var sendBuffer  = rawRequest.AsReadOnlyMemory();
                    if (env.logMessages) TransportUtils.LogMessage(Logger, $"client  ->", remoteHost, rawRequest);
                    // --- Send message
                    await socket.websocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                    
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
