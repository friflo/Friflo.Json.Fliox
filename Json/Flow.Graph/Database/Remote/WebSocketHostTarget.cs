// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
{
    // [Things I Wish Someone Told Me About ASP.NET Core WebSockets | codetinkerer.com] https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
    internal class WebSocketHostTarget : IEventTarget
    {
        private readonly WebSocket                          webSocket;
        /// Only set to true for testing. It avoids an early out at <see cref="Event.EventSubscriber.SendEvents"/> 
        private readonly bool                               fakeOpenClosedSocket;

        private readonly ChannelWriter<ArraySegment<byte>>  sendWriter;
        private readonly Task                               sendLoop;
        
        private WebSocketHostTarget (WebSocket webSocket, bool fakeOpenClosedSocket) {
            this.webSocket              = webSocket;
            this.fakeOpenClosedSocket   = fakeOpenClosedSocket;
            
            var opt = new UnboundedChannelOptions { SingleReader = true, SingleWriter = false };
            // opt.AllowSynchronousContinuations = true;
            var channel         = Channel.CreateUnbounded<ArraySegment<byte>>(opt);
            sendWriter          = channel.Writer;
            var sendReader      = channel.Reader;
            sendLoop            = SendLoop(sendReader);
        }
        
        // --- IEventTarget
        public bool IsOpen () {
            if (fakeOpenClosedSocket)
                return true;
            return webSocket.State == WebSocketState.Open;
        }

        public Task<bool> ProcessEvent(DatabaseEvent ev, SyncContext syncContext) {
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                var writer = pooledMapper.instance.writer;
                writer.WriteNullMembers = false;
                writer.Pretty           = true;
                var message         = new DatabaseMessage { ev = ev };
                var jsonMessage     = writer.Write(message);
                byte[] jsonBytes    = Encoding.UTF8.GetBytes(jsonMessage);
                try {
                    var arraySegment    = new ArraySegment<byte>(jsonBytes, 0, jsonBytes.Length);
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
        private Task SendLoop(ChannelReader<ArraySegment<byte>> sendReader) {
            var loopTask = Task.Run(async () => {
                try {
                    while (true) {
                        var sendMessage = await sendReader.ReadAsync();
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
        
        private async Task ReadLoop(MemoryStream memoryStream, RemoteHostDatabase remoteHost) {
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
                        var requestContent  = Encoding.UTF8.GetString(memoryStream.ToArray());
                        var contextPools    = new Pools(Pools.SharedPools);
                        var syncContext     = new SyncContext(contextPools, this);
                        var result          = await remoteHost.ExecuteRequestJson(requestContent, syncContext, ProtocolType.BiDirect).ConfigureAwait(false);
                        syncContext.pools.AssertNoLeaks();
                        byte[] resultBytes  = Encoding.UTF8.GetBytes(result.body);
                        var arraySegment    = new ArraySegment<byte>(resultBytes, 0, resultBytes.Length);
                        sendWriter.TryWrite(arraySegment);
                    }
                    continue;
                }
                Console.WriteLine($"ReadLoop() returns. WebSocket state: {state}");
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
                    await target.ReadLoop(memoryStream, remoteHost);
                }
                target.sendWriter.TryWrite(null);
                target.sendWriter.Complete();
            } catch (Exception e) {
                Debug.Fail("AcceptWebSocket() failed", e.Message);
            }
            await target.sendLoop;
        }
    }
}