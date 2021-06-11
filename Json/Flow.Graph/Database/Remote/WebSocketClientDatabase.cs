// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
        private readonly    string          endpoint;
        private readonly    ClientWebSocket websocket;

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
        }
        
        public async Task Close() {
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        protected override async Task<JsonResponse> ExecuteRequestJson(string jsonSyncRequest, SyncContext syncContext) {
            try {
                byte[] requestBytes  = Encoding.UTF8.GetBytes(jsonSyncRequest);
                var arraySegment    = new ArraySegment<byte>(requestBytes, 0, requestBytes.Length);
                await websocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                
                var         buffer      = new ArraySegment<byte>(new byte[8192]);
                using (var memoryStream = new MemoryStream()) {
                    if (websocket.State != WebSocketState.Open) {
                        return JsonResponse.CreateResponseError(syncContext, $"WebSocket not Open. {endpoint}", RequestStatusType.Error);
                    }
                    memoryStream.Position = 0;
                    memoryStream.SetLength(0);
                    WebSocketReceiveResult wsResult;
                    do {
                        wsResult = await websocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                        memoryStream.Write(buffer.Array, buffer.Offset, wsResult.Count);
                    }
                    while(!wsResult.EndOfMessage);
                    
                    var messageType = wsResult.MessageType;
                    if (messageType != WebSocketMessageType.Text) {
                        return JsonResponse.CreateResponseError(syncContext, $"Expect WebSocket message type text. type: {messageType} {endpoint}", RequestStatusType.Error);
                    }
                    var requestContent  = Encoding.UTF8.GetString(memoryStream.ToArray());
                    return new JsonResponse(requestContent, RequestStatusType.Ok);
                }
            }
            catch (Exception e) {
                var error = ResponseError.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                return JsonResponse.CreateResponseError(syncContext, error.ToString(), RequestStatusType.Exception);
            }
        }
    }
}