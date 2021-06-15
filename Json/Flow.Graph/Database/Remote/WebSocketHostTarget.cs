// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
{
    internal class WebSocketHostTarget : IEventTarget
    {
        private readonly WebSocket  webSocket;
        private readonly bool       fakeOpenClosedSocket;
        
        private WebSocketHostTarget (WebSocket webSocket, bool fakeOpenClosedSocket) {
            this.webSocket              = webSocket;
            this.fakeOpenClosedSocket   = fakeOpenClosedSocket;
        }
        
        // --- IEventTarget
        public bool IsOpen () {
            if (fakeOpenClosedSocket)
                return true;
            return webSocket.State == WebSocketState.Open;
        }

        public async Task<bool> ProcessEvent(DatabaseEvent ev, SyncContext syncContext) {
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                var writer = pooledMapper.instance.writer;
                writer.WriteNullMembers = false;
                writer.Pretty           = true;
                var message         = new DatabaseMessage { ev = ev };
                var jsonMessage     = writer.Write(message);
                byte[] jsonBytes    = Encoding.UTF8.GetBytes(jsonMessage);
                try {
                    var arraySegment    = new ArraySegment<byte>(jsonBytes, 0, jsonBytes.Length);
                    await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                    return true;
                }
                catch (Exception) {
                    return false;
                }
            }
        }
        
        internal static async Task AcceptWebSocket(HttpListenerContext ctx, RemoteHostDatabase remoteHost) {
            var         wsContext   = await ctx.AcceptWebSocketAsync(null).ConfigureAwait(false);
            WebSocket   websocket   = wsContext.WebSocket;
            var         buffer      = new ArraySegment<byte>(new byte[8192]);
            var         target      = new WebSocketHostTarget(websocket, remoteHost.fakeOpenClosedSockets);
            using (var memoryStream = new MemoryStream()) {
                while (true) {
                    switch (websocket.State) {
                        case WebSocketState.Open:
                            memoryStream.Position = 0;
                            memoryStream.SetLength(0);
                            WebSocketReceiveResult wsResult;
                            do {
                                wsResult = await websocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                                memoryStream.Write(buffer.Array, buffer.Offset, wsResult.Count);
                            }
                            while(!wsResult.EndOfMessage);
                            
                            if (wsResult.MessageType == WebSocketMessageType.Text) {
                                var requestContent  = Encoding.UTF8.GetString(memoryStream.ToArray());
                                var contextPools    = new Pools(Pools.SharedPools);
                                var syncContext     = new SyncContext(contextPools, target);
                                var result          = await remoteHost.ExecuteRequestJson(requestContent, syncContext, ProtocolType.BiDirect).ConfigureAwait(false);
                                syncContext.pools.AssertNoLeaks();
                                byte[] resultBytes  = Encoding.UTF8.GetBytes(result.body);
                                var arraySegment    = new ArraySegment<byte>(resultBytes, 0, resultBytes.Length);
                                await websocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                            }
                            break;
                        case WebSocketState.CloseReceived:
                            Console.WriteLine("WebSocket CloseReceived");
                            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                            return;
                        case WebSocketState.Closed:
                            Console.WriteLine("WebSocket Closed");
                            return;
                        case WebSocketState.Aborted:
                            Console.WriteLine("WebSocket Aborted");
                            return;
                    }
                }
            }
        }
    }
}