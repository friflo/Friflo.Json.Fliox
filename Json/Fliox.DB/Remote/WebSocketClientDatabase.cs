// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    public class WebSocketClientDatabase : RemoteClientDatabase
    {
        private             int                                         reqId;

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
                if (websocket.State != WebSocketState.Open) {
                    var error = $"websocket.State not WebSocketState.";
                    Console.WriteLine(error);
                    Debug.Fail(error);
                    // var error = JsonResponse.CreateResponseError(request.messageContext, $"WebSocket not Open. {endpoint}", ResponseStatusType.Error);
                    // request.response.SetResult(error);
                    return;
                }
                var contextPools    = new Pools(Pools.SharedPools);
                ProtocolMessage message = RemoteUtils.ReadProtocolMessage (messageJson, contextPools);
                if (message is ProtocolResponse resp) {
                    var requestId = resp.reqId;
                    if (!requestId.HasValue)
                        throw new InvalidOperationException("WebSocketClientDatabase requires reqId in response");
                    var id = requestId.Value;
                    if (!requests.TryRemove(id, out WebsocketRequest request)) {
                        throw new InvalidOperationException($"Expect corresponding request to response. id: {id}");
                    }
                    request.response.SetResult(resp);
                    return;
                }
                if (message is SubscriptionEvent ev) {
                    ProcessEvent(ev);
                }
            }
            catch (Exception e) {
                var error = $"OnReceive failed processing WebSocket message. Exception: {e}";
                Console.WriteLine(error);
                Debug.Fail(error);
            }
        }
        
        public override async Task<Response<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            int sendReqId = Interlocked.Increment(ref reqId);
            syncRequest.reqId = sendReqId;
            var jsonRequest = RemoteUtils.CreateProtocolMessage(syncRequest, messageContext.pools);
            try {
                // request need to be queued _before_ sending it to be prepared for handling the response.
                var wsRequest         = new WebsocketRequest(messageContext, cancellationToken);
                requests.TryAdd(sendReqId, wsRequest);
                
                var arraySegment    = jsonRequest.AsArraySegment();
                // --- Send message
                await websocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                
                // --- Wait for response
                var response = await wsRequest.response.Task.ConfigureAwait(false);
                if (response is SyncResponse syncResponse)
                    return new Response<SyncResponse>(syncResponse);
                return new Response<SyncResponse>($"invalid response: Was: {response.MessageType}");
            }
            catch (Exception e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                var msg = error.ToString();
                return new Response<SyncResponse>(msg);
            }
        }
    }
    
    internal class WebsocketRequest {
        internal readonly   MessageContext                          messageContext;
        internal readonly   TaskCompletionSource<ProtocolResponse>  response;          
        
        internal WebsocketRequest(MessageContext messageContext, CancellationTokenSource cancellationToken) {
            response            = new TaskCompletionSource<ProtocolResponse>();
            this.messageContext = messageContext;
            messageContext.canceler = () => {
                cancellationToken.Cancel();
            };
        }
    }
}
