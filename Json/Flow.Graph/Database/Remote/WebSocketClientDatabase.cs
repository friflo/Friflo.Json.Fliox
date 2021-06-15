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
        private  readonly   string                              endpoint;
        private             ClientWebSocket                     websocket;
        private  readonly   ConcurrentQueue<WebsocketRequest>   requestQueue = new ConcurrentQueue<WebsocketRequest>();


        public WebSocketClientDatabase(string endpoint) : base(ProtocolType.BiDirect) {
            this.endpoint = endpoint;
        }
        
        public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        }
        
        public async Task Connect() {
            var uri = new Uri(endpoint);
            if (websocket != null && websocket.State == WebSocketState.Open)
                throw new InvalidOperationException("websocket already in use");
            websocket = new ClientWebSocket();
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            _ = MessageReceiver();
        }
        
        public async Task Close() {
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
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
            try {
                var contextPools    = new Pools(Pools.SharedPools);
                using (var pooledMapper = contextPools.ObjectMapper.Get()) {
                    var reader = pooledMapper.instance.reader;
                    var message = reader.Read<DatabaseMessage>(messageJson);
                    var resp = message.resp; 
                    if (resp != null) {
                        if (!requestQueue.TryDequeue(out WebsocketRequest request)) {
                            throw new InvalidOperationException("Expect corresponding request to response");
                        }
                        if (websocket.State != WebSocketState.Open) {
                            var error = JsonResponse.CreateResponseError(request.syncContext, $"WebSocket not Open. {endpoint}", ResponseStatusType.Error);
                            request.response.SetResult(error);
                            return;
                        }
                        var writer = pooledMapper.instance.writer;
                        var responseJson = writer.Write(resp);
                        var response = new JsonResponse(responseJson, ResponseStatusType.Ok);
                        request.response.SetResult(response);
                        return;
                    }
                    var ev = message.ev;
                    if (ev != null) {
                        ProcessEvent(ev);
                    }
                }
            } catch (Exception e) {
                var error = $"OnReceive failed processing WebSocket message. Exception: {e}";
                Console.WriteLine(error);
                Debug.Fail(error);
            }
        }

        protected override async Task<JsonResponse> ExecuteRequestJson(string jsonSyncRequest, SyncContext syncContext) {
            try {
                byte[] requestBytes = Encoding.UTF8.GetBytes(jsonSyncRequest);
                var arraySegment    = new ArraySegment<byte>(requestBytes, 0, requestBytes.Length);
                await websocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                var request         = new WebsocketRequest(syncContext);
                requestQueue.Enqueue(request);
                
                var response = await request.response.Task.ConfigureAwait(false);
                return response;
            }
            catch (Exception e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                return JsonResponse.CreateResponseError(syncContext, error.ToString(), ResponseStatusType.Exception);
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
}
