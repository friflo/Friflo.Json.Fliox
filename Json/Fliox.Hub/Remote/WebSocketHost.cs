// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Threading;

namespace Friflo.Json.Fliox.Hub.Remote
{
    // [Things I Wish Someone Told Me About ASP.NET Core WebSockets | codetinkerer.com] https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
    public sealed class WebSocketHost : IDisposable, IEventReceiver, ILogSource
    {
        private  readonly   WebSocket                               webSocket;
        /// Only set to true for testing. It avoids an early out at <see cref="EventSubClient.SendEvents"/> 
        private  readonly   bool                                    fakeOpenClosedSocket;

        private  readonly   DataChannelSlim<ArraySegment<byte>>     channel;
        private  readonly   IDataChannelWriter<ArraySegment<byte>>  sendWriter;
        private  readonly   IDataChannelReader<ArraySegment<byte>>  sendReader;
        private  readonly   Pool                                    pool;
        private  readonly   SharedCache                             sharedCache;
        private  readonly   IPEndPoint                              remoteEndPoint;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger                              Logger { get; }

        
        private WebSocketHost (SharedEnv env, WebSocket webSocket, IPEndPoint remoteEndPoint, bool fakeOpenClosedSocket) {
            pool                        = env.Pool;
            sharedCache                 = env.sharedCache;
            Logger                      = env.hubLogger;
            this.webSocket              = webSocket;
            this.remoteEndPoint         = remoteEndPoint;
            this.fakeOpenClosedSocket   = fakeOpenClosedSocket;
            
            channel     = DataChannelSlim<ArraySegment<byte>>.CreateUnbounded(true, false);
            sendWriter  = channel.Writer;
            sendReader  = channel.Reader;
        }

        public void Dispose() {
            channel.Dispose();
        }

        // --- IEventReceiver
        public bool IsRemoteTarget ()   => true;
        public bool IsOpen () {
            if (fakeOpenClosedSocket)
                return true;
            return webSocket.State == WebSocketState.Open;
        }

        public Task<bool> ProcessEvent(ProtocolEvent ev) {
            try {
                var pooledMapper    = pool.ObjectMapper;
                var jsonEvent       = RemoteUtils.CreateProtocolMessage(ev, pooledMapper);
                var arraySegment    = jsonEvent.AsArraySegment();
                sendWriter.TryWrite(arraySegment);
                return Task.FromResult(true);
            }
            catch (Exception) {
                return Task.FromResult(false);
            }
        }
        
        // Send queue (sendWriter / sendReader) is required  to prevent having more than one WebSocket.SendAsync() call outstanding.
        // Otherwise:
        // System.InvalidOperationException: There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time. 
        private Task RunSendLoop() {
            var loopTask = Task.Run(async () => {
                try {
                    while (true) {
                        var sendMessage = await sendReader.ReadAsync().ConfigureAwait(false);
                        if (sendMessage == null)
                            return;
                        // if (sendMessage.Count > 100000) Console.WriteLine($"SendLoop. size: {sendMessage.Count}");
                        await webSocket.SendAsync(sendMessage, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                    }
                } catch (Exception e) {
                    var msg = GetExceptionMessage("WebSocketHost.SendLoop()", remoteEndPoint, e);
                    Logger.Log(HubLog.Info, msg);
                }
            });
            return loopTask;
        }
        
        private async Task RunReceiveLoop(RemoteHost remoteHost) {
            var memoryStream    = new MemoryStream();
            var buffer          = new ArraySegment<byte>(new byte[8192]);
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
                        var requestContent  = new JsonValue(memoryStream.ToArray());
                        var syncContext     = new SyncContext(pool, this, sharedCache);
                        var result          = await remoteHost.ExecuteJsonRequest(requestContent, syncContext).ConfigureAwait(false);
                        
                        syncContext.Release();
                        var arraySegment    = result.body.AsArraySegment();
                        sendWriter.TryWrite(arraySegment);
                    }
                    continue;
                }
                // Logger.Log(HubLog.Info, $"receive loop finished. WebSocket state: {state}, remote: {remoteEndPoint}");
                if (state == WebSocketState.CloseReceived) {
                    var description = webSocket.CloseStatusDescription;
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, description, CancellationToken.None).ConfigureAwait(false);    
                }
                return;
            }
        }
        
        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="Logger"/> <br/>
        /// </summary>
        public static async Task SendReceiveMessages(WebSocket websocket, IPEndPoint remoteEndPoint, RemoteHost remoteHost)
        {
            var  target     = new WebSocketHost(remoteHost.sharedEnv, websocket, remoteEndPoint, remoteHost.fakeOpenClosedSockets);
            Task sendLoop   = null;
            try {
                sendLoop = target.RunSendLoop();

                await target.RunReceiveLoop(remoteHost).ConfigureAwait(false);

                target.sendWriter.TryWrite(default);
                target.sendWriter.Complete();
            }
            catch (WebSocketException e) {
                var msg = GetExceptionMessage("WebSocketHost.SendReceiveMessages()", remoteEndPoint, e);
                remoteHost.Logger.Log(HubLog.Info, msg);
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("WebSocketHost.SendReceiveMessages()", remoteEndPoint, e);
                remoteHost.Logger.Log(HubLog.Info, msg);
            }
            finally {
                if (sendLoop != null) {
                    await sendLoop.ConfigureAwait(false);
                }
                target.Dispose();
                websocket.Dispose();
            }
        }
        
        private static string GetExceptionMessage(string location, IPEndPoint remoteEndPoint, Exception e) {
            if (e.InnerException is HttpListenerException listenerException) {
                e = listenerException;
                // observed ErrorCode:
                // 995 The I/O operation has been aborted because of either a thread exit or an application request.
                return $"{location} {e.GetType().Name}: {e.Message} ErrorCode: {listenerException.ErrorCode}, remote: {remoteEndPoint} ";
            }
            if (e is WebSocketException wsException) {
                // e.g. WebSocketException - ErrorCode: 0, HResult: 0x80004005, WebSocketErrorCode: ConnectionClosedPrematurely, Message:The remote party closed the WebSocket connection without completing the close handshake., remote:[::1]:51809
                return $"{location} {e.GetType().Name} {e.Message} ErrorCode: {wsException.ErrorCode}, HResult: 0x{e.HResult:X}, WebSocketErrorCode: {wsException.WebSocketErrorCode}, remote: {remoteEndPoint}";
            }
            return $"{location} {e.GetType().Name}: {e.Message}, remote: {remoteEndPoint}";
        }
    }
}