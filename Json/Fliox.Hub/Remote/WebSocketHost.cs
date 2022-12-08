// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Remote
{
    // [Things I Wish Someone Told Me About ASP.NET Core WebSockets | codetinkerer.com] https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
    public sealed class WebSocketHost : EventReceiver, IDisposable, ILogSource
    {
        private  readonly   WebSocket                           webSocket;
        /// Only set to true for testing. It avoids an early out at <see cref="EventSubClient.SendEvents"/> 
        private  readonly   bool                                fakeOpenClosedSocket;

        private  readonly   MessageBufferQueue                  sendQueue;
        private  readonly   List<MessageBuffer>                 messages;
        
        private  readonly   Pool                                pool;
        private  readonly   SharedEnv                           sharedEnv;
        private  readonly   IPEndPoint                          remoteEndPoint;
        private  readonly   TypeStore                           typeStore;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger                          Logger { get; }

        
        private WebSocketHost (SharedEnv env, WebSocket webSocket, IPEndPoint remoteEndPoint, bool fakeOpenClosedSocket) {
            pool                        = env.Pool;
            sharedEnv                   = env;
            Logger                      = env.hubLogger;
            typeStore                   = env.TypeStore;
            this.webSocket              = webSocket;
            this.remoteEndPoint         = remoteEndPoint;
            this.fakeOpenClosedSocket   = fakeOpenClosedSocket;
            
            sendQueue                   = new MessageBufferQueue();
            messages                    = new List<MessageBuffer>();
        }

        public void Dispose() {
            sendQueue.Dispose();
        }

        // --- IEventReceiver
        public override bool    IsRemoteTarget ()   => true;
        public override bool    IsOpen () {
            if (fakeOpenClosedSocket)
                return true;
            return webSocket.State == WebSocketState.Open;
        }

        public override void SendEvent(EventMessage eventMessage, bool reusedEvent, in SendEventArgs args) {
            try {
                var bytesEvent  = RemoteUtils.CreateProtocolEvent(eventMessage, args);
                sendQueue.Enqueue(bytesEvent);
            }
            catch (Exception e) {
               Logger.Log(HubLog.Error, "WebSocketHost.SendEvent", e);
            }
        }
        
        private  static readonly   Regex   RegExLineFeed   = new Regex(@"\s+");
        private  static readonly   bool    LogMessage      = false;
        
        // Send queue (sendWriter / sendReader) is required  to prevent having more than one WebSocket.SendAsync() call outstanding.
        // Otherwise:
        // System.InvalidOperationException: There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time. 
        private Task RunSendLoop() {
            var loopTask = Task.Run(async () => {
                try {
                    while (true) {
                        var remoteEvent = await sendQueue.DequeMessages(messages).ConfigureAwait(false);
                        foreach (var message in messages) {
                            if (LogMessage) {
                                var msg = RegExLineFeed.Replace(message.AsString(), "");
                                Logger.Log(HubLog.Info, msg);
                            }
                            var arraySegment = message.AsArraySegment();
                            // if (sendMessage.Count > 100000) Console.WriteLine($"SendLoop. size: {sendMessage.Count}");
                            await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                        }
                        if (remoteEvent == MessageBufferEvent.Closed) {
                            return;
                        }
                    }
                } catch (Exception e) {
                    var msg = GetExceptionMessage("WebSocketHost.SendLoop()", remoteEndPoint, e);
                    Logger.Log(HubLog.Info, msg);
                }
            });
            return loopTask;
        }
        
        private async Task RunReceiveLoop(RemoteHost remoteHost, ObjectMapper mapper) {
            var memoryStream            = new MemoryStream();
            var buffer                  = new ArraySegment<byte>(new byte[8192]);
            var syncBuffers             = new SyncBuffers(new List<SyncRequestTask>());
            var syncContext             = new SyncContext(sharedEnv, this, syncBuffers); // reused context
            var memoryBuffer            = new MemoryBuffer(4 * 1024);
            // mapper.reader.InstancePool  = new InstancePool(typeStore);    // reused SyncRequest
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
                        var requestContent  = new JsonValue(memoryStream.GetBuffer(), (int)memoryStream.Position);
                        syncContext.Init();
                        syncContext.SetMemoryBuffer(memoryBuffer);
                        mapper.reader.InstancePool?.Reuse();
                        // inlined ExecuteJsonRequest() to avoid async call:
                        // JsonResponse response = await remoteHost.ExecuteJsonRequest(mapper, requestContent, syncContext).ConfigureAwait(false);
                        JsonResponse response;
                        try {
                            var syncRequest = RemoteUtils.ReadSyncRequest(mapper, requestContent, out string error);
                            if (error != null) {
                                response = JsonResponse.CreateError(mapper, error, ErrorResponseType.BadResponse, null);
                            } else {
                                var syncResult = await remoteHost.localHub.ExecuteRequestAsync(syncRequest, syncContext).ConfigureAwait(false);
                
                                response = RemoteHost.CreateJsonResponse(syncResult, syncRequest.reqId, mapper);
                            }
                        }
                        catch (Exception e) {
                            var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                            response = JsonResponse.CreateError(mapper, errorMsg, ErrorResponseType.Exception, null);
                        }
                        sendQueue.Enqueue(response.body); // Enqueue() copy the result.body array
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

                using (var pooledMapper = target.pool.ObjectMapper.Get()) {
                    await target.RunReceiveLoop(remoteHost, pooledMapper.instance).ConfigureAwait(false);
                }

                target.sendQueue.Close();
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