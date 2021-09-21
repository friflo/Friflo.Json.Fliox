// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL.Remote
{
    public class WebSocketClientDatabase : RemoteClientDatabase
    {
        private  readonly   string                                      endpoint;
        private             ClientWebSocket                             websocket;
        private  readonly   ConcurrentDictionary<int, WebsocketRequest> requests = new ConcurrentDictionary<int, WebsocketRequest>();
        private  readonly   CancellationTokenSource                     cancellationToken = new CancellationTokenSource();


        public WebSocketClientDatabase(string endpoint) {
            this.endpoint = endpoint;
        }
        
        /* public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        } */
        
        public async Task Connect() {
            var uri = new Uri(endpoint);
            if (websocket != null && websocket.State == WebSocketState.Open) {
                throw new InvalidOperationException("websocket already in use");
            }
            // clear request queue is required for reconnects 
            requests.Clear();
            
            websocket = new ClientWebSocket();
            // websocket.Options.SetBuffer(4096, 4096);
            
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            try {
                _ = ReceiveLoop();
            } catch (Exception e) {
                Debug.Fail("ReceiveLoop() failed", e.Message);
            }
        }
        
        public async Task Close() {
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }

        private async Task ReceiveLoop() {
            var        buffer       = new ArraySegment<byte>(new byte[8192]);
            using (var memoryStream = new MemoryStream()) {
                while (true) {
                    memoryStream.Position = 0;
                    memoryStream.SetLength(0);
                    try {
                        WebSocketReceiveResult wsResult;
                        do {
                            if (websocket.State != WebSocketState.Open) {
                                Console.WriteLine($"Pre-ReceiveAsync. State: {websocket.State}");
                                return;
                            }
                            wsResult = await websocket.ReceiveAsync(buffer, cancellationToken.Token).ConfigureAwait(false);
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
                        var requestContent  = new JsonUtf8(memoryStream.ToArray());
                        OnReceive (requestContent);
                    } catch (Exception) {
                        foreach (var pair in requests) {
                            var request = pair.Value;
                            request.response.SetCanceled();
                        }
                    }
                }
            }
        }
        
        private void OnReceive(JsonUtf8 messageJson) {
            try {
                var contextPools    = new Pools(Pools.SharedPools);
                DatabaseMessage message;
                using (var pooledMapper = contextPools.ObjectMapper.Get()) {
                    var reader = pooledMapper.instance.reader;
                    message = reader.Read<DatabaseMessage>(messageJson);
                }
                if (message is SyncResponse resp) {
                    var requestId = resp.reqId;
                    if (!requestId.HasValue)
                        throw new InvalidOperationException("WebSocketClientDatabase requires reqId in response");
                    var id = requestId.Value;
                    if (!requests.TryRemove(id, out WebsocketRequest request)) {
                        throw new InvalidOperationException($"Expect corresponding request to response. id: {id}");
                    }
                    if (websocket.State != WebSocketState.Open) {
                        var error = JsonResponse.CreateResponseError(request.messageContext, $"WebSocket not Open. {endpoint}", ResponseStatusType.Error);
                        request.response.SetResult(error);
                        return;
                    }
                    // var writer          = pooledMapper.instance.writer;
                    // var responseJson    = new JsonUtf8(writer.WriteAsArray<DatabaseMessage>(resp));
                    var response        = new JsonResponse(messageJson, ResponseStatusType.Ok);
                    request.response.SetResult(response);
                    return;
                }
                if (message is SubscriptionEvent ev) {
                    ProcessEvent(ev);
                }
                
            } catch (Exception e) {
                var error = $"OnReceive failed processing WebSocket message. Exception: {e}";
                Console.WriteLine(error);
                Debug.Fail(error);
            }
        }

        protected override async Task<JsonResponse> ExecuteRequestJson(int requestId, JsonUtf8 jsonSyncRequest, MessageContext messageContext) {
            if (requestId < 1)
                throw new InvalidOperationException("Expect requestId > 0");
            try {
                // request need to be queued _before_ sending it to be prepared for handling the response.
                var request         = new WebsocketRequest(messageContext, cancellationToken);
                requests.TryAdd(requestId, request);
                
                var arraySegment    = jsonSyncRequest.AsArraySegment();
                await websocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                
                var response = await request.response.Task.ConfigureAwait(false);
                return response;
            }
            catch (Exception e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                return JsonResponse.CreateResponseError(messageContext, error.ToString(), ResponseStatusType.Exception);
            }
        }
    }
    
    internal class WebsocketRequest {
        internal readonly   MessageContext                      messageContext;
        internal readonly   TaskCompletionSource<JsonResponse>  response;          
        
        internal WebsocketRequest(MessageContext messageContext, CancellationTokenSource cancellationToken) {
            response            = new TaskCompletionSource<JsonResponse>();
            this.messageContext = messageContext;
            messageContext.canceler = () => {
                cancellationToken.Cancel();
            };
        }
    }
}
