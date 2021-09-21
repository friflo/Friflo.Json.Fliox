// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Host.Utils;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    // [Things I Wish Someone Told Me About ASP.NET Core WebSockets | codetinkerer.com] https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
    internal class WebSocketHostTarget : IEventTarget
    {
        private  readonly   WebSocket                               webSocket;
        /// Only set to true for testing. It avoids an early out at <see cref="EventSubscriber.SendEvents"/> 
        private  readonly   bool                                    fakeOpenClosedSocket;

        private  readonly   DataChannelWriter<ArraySegment<byte>>   sendWriter;
        private  readonly   Task                                    sendLoop;
        private  readonly   Pools                                   pools = new Pools(Pools.SharedPools);
        
        private WebSocketHostTarget (WebSocket webSocket, bool fakeOpenClosedSocket) {
            this.webSocket              = webSocket;
            this.fakeOpenClosedSocket   = fakeOpenClosedSocket;
            
            var channel         = DataChannel<ArraySegment<byte>>.CreateUnbounded(true, false);
            sendWriter          = channel.writer;
            var sendReader      = channel.reader;
            sendLoop            = SendLoop(sendReader);
        }
        
        // --- IEventTarget
        public bool IsOpen () {
            if (fakeOpenClosedSocket)
                return true;
            return webSocket.State == WebSocketState.Open;
        }

        public Task<bool> ProcessEvent(ProtocolEvent ev, MessageContext messageContext) {
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                var writer = pooledMapper.instance.writer;
                writer.WriteNullMembers = false;
                writer.Pretty           = true;
                var jsonMessage         = new JsonUtf8(writer.WriteAsArray<ProtocolMessage>(ev));
                try {
                    var arraySegment    = jsonMessage.AsArraySegment();
                    sendWriter.TryWrite(arraySegment);
                    return Task.FromResult(true);
                }
                catch (Exception) {
                    return Task.FromResult(false);
                }
            }
        }
        
        // Send queue (sendWriter / sendReader) is required  to prevent having more than one WebSocket.SendAsync() call outstanding.
        // Otherwise:
        // System.InvalidOperationException: There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time. 
        private Task SendLoop(DataChannelReader<ArraySegment<byte>> sendReader) {
            var loopTask = Task.Run(async () => {
                try {
                    while (true) {
                        var sendMessage = await sendReader.ReadAsync().ConfigureAwait(false);
                        if (sendMessage == null)
                            return;
                        await webSocket.SendAsync(sendMessage, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                    }
                } catch (Exception e) {
                    Debug.Fail("SendLoop() failed", e.Message);
                }
            });
            return loopTask;
        }
        
        private async Task ReceiveLoop(MemoryStream memoryStream, RemoteHostDatabase remoteHost) {
            var         buffer      = new ArraySegment<byte>(new byte[8192]);
            while (true) {
                var state = webSocket.State;
                if (state == WebSocketState.Open) {
                    memoryStream.Position = 0;
                    memoryStream.SetLength(0);
                    WebSocketReceiveResult wsResult;
                    do {
                        wsResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                        memoryStream.Write(buffer.Array, buffer.Offset, wsResult.Count);
                    }
                    while(!wsResult.EndOfMessage);
                    
                    if (wsResult.MessageType == WebSocketMessageType.Text) {
                        var requestContent  = new JsonUtf8(memoryStream.ToArray());
                        var messageContext  = new MessageContext(pools, this);
                        var result          = await remoteHost.ExecuteRequestJson2(requestContent, messageContext).ConfigureAwait(false);
                        messageContext.Release();
                        var arraySegment    = result.body.AsArraySegment();
                        sendWriter.TryWrite(arraySegment);
                    }
                    continue;
                }
                Console.WriteLine($"ReceiveLoop() returns. WebSocket state: {state}");
                if (state == WebSocketState.CloseReceived) {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);    
                }
                return;
            }
        }
        
        internal static async Task AcceptWebSocket(HttpListenerContext ctx, RemoteHostDatabase remoteHost) {
            var         wsContext   = await ctx.AcceptWebSocketAsync(null).ConfigureAwait(false);
            WebSocket   websocket   = wsContext.WebSocket;
            var         target      = new WebSocketHostTarget(websocket, remoteHost.fakeOpenClosedSockets);
            try {
                using (var memoryStream = new MemoryStream()) {
                    await target.ReceiveLoop(memoryStream, remoteHost).ConfigureAwait(false);
                }
                target.sendWriter.TryWrite(default);
                target.sendWriter.Complete();
            } catch (Exception e) {
                Debug.Fail("AcceptWebSocket() failed", e.Message);
            }
            await target.sendLoop.ConfigureAwait(false);
        }
    }
}