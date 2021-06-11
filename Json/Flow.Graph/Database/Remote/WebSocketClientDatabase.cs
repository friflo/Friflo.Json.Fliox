// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
{
    public class WebSocketClientDatabase : RemoteClientDatabase
    {
        private readonly    string                              endpoint;
        private readonly    ClientWebSocket                     websocket;
        private readonly    ConcurrentQueue<WebsocketRequest>   requestQueue = new ConcurrentQueue<WebsocketRequest>();


        public WebSocketClientDatabase(string endpoint) {
            this.endpoint = endpoint;
            websocket = new ClientWebSocket();
        }
        
        public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
            websocket.Dispose();
        }
        
        public async Task Connect() {
            var uri = new Uri(endpoint);
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            _ = MessageReceiver();
        }
        
        public async Task Close() {
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        
        private async Task MessageReceiver() {
            var        buffer       = new ArraySegment<byte>(new byte[8192]);
            using (var memoryStream = new MemoryStream()) {
                while (true) {
                    memoryStream.Position = 0;
                    memoryStream.SetLength(0);
                    WebSocketReceiveResult wsResult;
                    
                    do {
                        if (websocket.State != WebSocketState.Open) {
                            Console.WriteLine($"Pre-ReceiveAsync. State: {websocket.State}");
                            return;
                        }
                        wsResult = await websocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                        memoryStream.Write(buffer.Array, buffer.Offset, wsResult.Count);
                    }
                    while(!wsResult.EndOfMessage);
                    
                    if (websocket.State != WebSocketState.Open) {
                        Console.WriteLine($"Post-ReceiveAsync. State: {websocket.State}");
                        return;
                    }
                    var messageType = wsResult.MessageType;
                    if (messageType != WebSocketMessageType.Text) {
                        Console.WriteLine($"Expect WebSocket message type text. type: {messageType} {endpoint}");
                        continue;
                    }
                    var requestContent  = Encoding.UTF8.GetString(memoryStream.ToArray());
                    OnReceive (requestContent); 
                }
            }
        }
        
        private void OnReceive(string messageJson) {
            var contextPools    = new Pools(Pools.SharedPools);
            using (var pooledMapper = contextPools.ObjectMapper.Get()) {
                var reader = pooledMapper.instance.reader;
                try {
                    var message = reader.Read<WebSocketMessage>(messageJson);
                    if (message.resp != null) {
                        if (requestQueue.TryDequeue(out WebsocketRequest request)) {
                            if (websocket.State != WebSocketState.Open) {
                                var error = JsonResponse.CreateResponseError(request.syncContext, $"WebSocket not Open. {endpoint}", RequestStatusType.Error);
                                request.response.SetResult(error);
                                return;
                            }
                            var writer = pooledMapper.instance.writer;
                            var responseJson = writer.Write(message.resp);
                            var response = new JsonResponse(responseJson, RequestStatusType.Ok);
                            request.response.SetResult(response);
                            return;
                        }
                        return;
                    }
                    if (message.ev != null) {
                        throw new NotImplementedException("");
                    }
                } catch (Exception e) {
                    var error = $"OnReceive failed processing WebSocket message. Exception: {e}";
                    Console.WriteLine(error);
                    Debug.Fail(error);
                }
            }
        }

        protected override async Task<JsonResponse> ExecuteRequestJson(string jsonSyncRequest, SyncContext syncContext) {
            try {
                byte[] requestBytes = Encoding.UTF8.GetBytes(jsonSyncRequest);
                var arraySegment    = new ArraySegment<byte>(requestBytes, 0, requestBytes.Length);
                await websocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                var request         = new WebsocketRequest(syncContext);
                requestQueue.Enqueue(request);
                
                var response = await request.response.Task;
                return response;
            }
            catch (Exception e) {
                var error = ResponseError.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                return JsonResponse.CreateResponseError(syncContext, error.ToString(), RequestStatusType.Exception);
            }
        }
    }
    
    internal class WebsocketRequest {
        internal readonly   SyncContext                         syncContext;
        internal readonly   TaskCompletionSource<JsonResponse>  response;          
        
        internal WebsocketRequest(SyncContext syncContext) {
            response            = new TaskCompletionSource<JsonResponse>(); 
            this.syncContext    = syncContext;
        }
    }
    
    public class WebSocketMessage
    {
        public DatabaseResponse resp;
        public DatabaseEvent    ev;
    }
}