// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// Each <see cref="WebSocketConnection"/> store its send <see cref="requests"/> to map a 
    /// received <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>
    /// </summary>
    internal sealed class WebSocketConnection
    {
        internal  readonly  ClientWebSocket                             websocket;
        internal  readonly  ConcurrentDictionary<int, WebsocketRequest> requests;
        
        internal WebSocketConnection() {
            websocket   = new ClientWebSocket();
            requests    = new ConcurrentDictionary<int, WebsocketRequest>();
        }
    }
    
    /// <summary>
    /// A <see cref="FlioxHub"/> accessed remotely  using a <see cref="WebSocket"/> connection
    /// </summary>
    public sealed class WebSocketClientHub : RemoteClientHub
    {
        private  readonly   string                      endpoint;
        private  readonly   Uri                         endpointUri;
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public              bool                        IsConnected => wsConnection?.websocket.State == WebSocketState.Open;

        /// lock (<see cref="websocketLock"/>) {
        private readonly    object                      websocketLock = new object();
        private             WebSocketConnection         wsConnection;
        private             Task<WebSocketConnection>   connectTask;
        // }
        
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        
        public   override   string                      ToString() => $"{database.name} - endpoint: {endpoint}";
        
        public WebSocketClientHub(string dbName, string endpoint, SharedEnv env = null)
            : base(new RemoteDatabase(dbName), env)
        {
            this.endpoint   = endpoint;
            endpointUri     = new Uri(endpoint);
        }
        
        /* public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        } */
        
        private WebSocketConnection GetWebsocketConnection() {
            lock (websocketLock) {
                var wsConn = wsConnection;
                if (wsConn != null && wsConn.websocket.State == WebSocketState.Open)
                    return wsConn;
                return null;
            }
        }
        
        private Task<WebSocketConnection> JoinConnects(out TaskCompletionSource<WebSocketConnection> tcs, out WebSocketConnection wsConn) {
            lock (websocketLock) {
                if (connectTask != null) {
                    wsConn  = null;
                    tcs     = null;
                    return connectTask;
                }
                wsConn  = wsConnection = new WebSocketConnection();
                tcs     = new TaskCompletionSource<WebSocketConnection>();
                connectTask = tcs.Task;
                return connectTask;
            }
        }
        
        // static int count;
        
        private async Task<WebSocketConnection> Connect() {
            var task = JoinConnects(out var tcs, out WebSocketConnection wsConn);
            if (tcs == null) {
                wsConn = await task.ConfigureAwait(false);
                return wsConn;
            }

            // Console.WriteLine($"WebSocketClientHub.Connect() endpoint: {endpoint}");
            // ws.Options.SetBuffer(4096, 4096);
            try {
                // Console.WriteLine($"Connect {++count}");
                await wsConn.websocket.ConnectAsync(endpointUri, CancellationToken.None).ConfigureAwait(false);

                connectTask = null;
                tcs.SetResult(wsConn);
            } catch (Exception e) {
                connectTask = null;
                tcs.SetException(e);
                throw;
            }
            try {
                _ = ReceiveLoop(wsConn);
            } catch (Exception e) {
                Debug.Fail("ReceiveLoop() failed", e.Message);
            }
            return wsConn;
        }
        
        public async Task Close() {
            WebSocketConnection wsConn;
            lock (websocketLock) {
                wsConn = wsConnection;
                if (wsConn == null || wsConn.websocket.State == WebSocketState.Closed)
                    return;
                wsConnection = null;
            }
            await wsConn.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }

        private async Task ReceiveLoop(WebSocketConnection wsConn) {
            var buffer  = new ArraySegment<byte>(new byte[8192]);
            var ws      = wsConn.websocket;
            using (var memoryStream = new MemoryStream()) {
                while (true) {
                    memoryStream.Position = 0;
                    memoryStream.SetLength(0);
                    try {
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
                        var messageType = wsResult.MessageType;
                        if (messageType != WebSocketMessageType.Text) {
                            Logger.Log(HubLog.Error, $"Expect WebSocket message type text. type: {messageType} {endpoint}");
                            continue;
                        }
                        var requestContent  = new JsonValue(memoryStream.GetBuffer(), (int)memoryStream.Position);
                        OnReceive (wsConn, requestContent);
                    } catch (Exception e) {
                        Logger.Log(HubLog.Error, e.Message);
                        foreach (var pair in wsConn.requests) {
                            var request = pair.Value;
                            request.response.SetCanceled();
                        }
                    }
                }
            }
        }
        
        private void OnReceive(WebSocketConnection wsConn, in JsonValue messageJson) {
            try {
                var pooledMapper        = sharedEnv.Pool.ObjectMapper;
                // if (messageJson.Length > 100000) Console.WriteLine($"OnReceive. size: {messageJson.Length}");
                ProtocolMessage message = RemoteUtils.ReadProtocolMessage (messageJson, pooledMapper, out _);
                switch (message) {
                    case null:
                        break; // errors are ignored. 
                    case ProtocolResponse resp:
                        var responseReqId = resp.reqId;
                        if (!responseReqId.HasValue)
                            throw new InvalidOperationException($"WebSocketClientDatabase requires reqId in response:\n{messageJson}");
                        var id = responseReqId.Value;
                        if (!wsConn.requests.TryRemove(id, out WebsocketRequest request)) {
                            throw new InvalidOperationException($"Expect corresponding request to response. id: {id}");
                        }
                        request.response.SetResult(resp);
                        return;
                    case EventMessage ev:
                        OnReceiveEvent(ev);
                        break;
                }
            }
            catch (Exception e) {
                var message = "OnReceive failed processing WebSocket message";
                Logger.Log(HubLog.Error, message, e);
                Debug.Fail($"{message}, exception: {e}");
            }
        }
        
        public override async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            var wsConn = GetWebsocketConnection();
            if (wsConn == null) {
                wsConn = await Connect().ConfigureAwait(false);
            }
            int sendReqId       = Interlocked.Increment(ref reqId);
            syncRequest.reqId   = sendReqId;
            var jsonRequest     = RemoteUtils.CreateProtocolMessage(syncRequest, syncContext.ObjectMapper);

            try {
                // request need to be queued _before_ sending it to be prepared for handling the response.
                var wsRequest       = new WebsocketRequest(syncContext, cancellationToken);
                wsConn.requests.TryAdd(sendReqId, wsRequest);
                var buffer    = jsonRequest.AsArraySegment();

                // --- Send message
                await wsConn.websocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                
                // --- Wait for response
                var response = await wsRequest.response.Task.ConfigureAwait(false);
                
                if (response is SyncResponse syncResponse) {
                    return new ExecuteSyncResult(syncResponse);
                }
                if (response is ErrorResponse errorResponse) {
                    return new ExecuteSyncResult(errorResponse.message, errorResponse.type);
                }
                return new ExecuteSyncResult($"invalid response: Was: {response.MessageType}", ErrorResponseType.BadResponse);
            }
            catch (Exception e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                var msg = error.ToString();
                return new ExecuteSyncResult(msg, ErrorResponseType.Exception);
            }
        }
    }
    
    internal readonly struct WebsocketRequest
    {
        internal readonly   TaskCompletionSource<ProtocolResponse>  response;          
        
        internal WebsocketRequest(SyncContext syncContext, CancellationTokenSource cancellationToken) {
            response            = new TaskCompletionSource<ProtocolResponse>();
            syncContext.canceler = () => {
                cancellationToken.Cancel();
            };
        }
    }
}
