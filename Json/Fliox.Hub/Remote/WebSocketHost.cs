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
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Fliox.Hub.Remote
{
    // [Things I Wish Someone Told Me About ASP.NET Core WebSockets | codetinkerer.com] https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
    public sealed class WebSocketHost : EventReceiver, IDisposable, ILogSource
    {
        private  readonly   WebSocket               webSocket;
        /// Only set to true for testing. It avoids an early out at <see cref="EventSubClient.SendEvents"/> 
        private  readonly   bool                    fakeOpenClosedSocket;

        private  readonly   MessageBufferQueueAsync<VoidMeta> sendQueue;
        private  readonly   List<JsonValue>         messages;
        
        private  readonly   FlioxHub                hub;
        private  readonly   Pool                    pool;
        private  readonly   SharedEnv               sharedEnv;
        private  readonly   IPEndPoint              remoteEndPoint;
        private  readonly   TypeStore               typeStore;
        private  readonly   HostMetrics             hostMetrics;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger                          Logger { get; }

        
        private WebSocketHost (
            RemoteHost      remoteHost,
            WebSocket       webSocket,
            IPEndPoint      remoteEndPoint)
        {
            var env                     = remoteHost.sharedEnv;
            hub                         = remoteHost.localHub;
            pool                        = env.Pool;
            sharedEnv                   = env;
            Logger                      = env.hubLogger;
            typeStore                   = env.TypeStore;
            this.webSocket              = webSocket;
            this.remoteEndPoint         = remoteEndPoint;
            this.fakeOpenClosedSocket   = remoteHost.fakeOpenClosedSockets;
            this.hostMetrics            = remoteHost.metrics;
            
            sendQueue                   = new MessageBufferQueueAsync<VoidMeta>();
            messages                    = new List<JsonValue>();
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

        public override void SendEvent(in RemoteEvent eventMessage) {
            try {
                sendQueue.AddTail(eventMessage.message);
            }
            catch (Exception e) {
               Logger.Log(HubLog.Error, "WebSocketHost.SendEvent", e);
            }
        }
        
        private  static readonly   Regex   RegExLineFeed   = new Regex(@"\s+");
        private  static readonly   bool    LogMessage      = false;
        
        /// <summary>
        /// Loop is purely I/O bound => don't wrap in
        /// return Task.Run(async () => { ... });
        /// </summary>
        /// <remarks>
        /// A send loop reading from a queue is required as message can be sent from two different sources <br/>
        /// 1. response messages created in <see cref="ReceiveMessageLoop"/> <br/>
        /// 2. event messages send to <see cref="EventSubClient"/>'s <br/>
        /// The loop ensures a WebSocket.SendAsync() is called only once at a time.
        /// </remarks>
        /// <seealso cref="WebSocketHost.RunReceiveMessageLoop"/>
        private async Task RunSendMessageLoop() {
            try {
                await SendMessageLoop();
            } catch (Exception e) {
                var msg = GetExceptionMessage("RunSendMessageLoop()", remoteEndPoint, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        // Send queue (sendWriter / sendReader) is required  to prevent having more than one WebSocket.SendAsync() call outstanding.
        // Otherwise:
        // System.InvalidOperationException: There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time. 
        private async Task SendMessageLoop() {
            while (true) {
                var remoteEvent = await sendQueue.DequeMessagesAsync(messages).ConfigureAwait(false);
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
        }
        
        /// <summary>
        /// Loop is I/O bound and CPU bound (parse request, execute request, create response) => don't wrap in
        /// return Task.Run(async () => { ... });
        /// <br/>
        /// As recommended in [... Don't Use Task.Run in the Implementation] <br/>
        /// "They concluded that the best solution is to use an asynchronous signature
        /// but document the method clearly so that its CPU-bound nature will not be surprising" <br/>
        /// <br/>
        /// See. [Should I expose asynchronous wrappers for synchronous methods? - .NET Parallel Programming]
        ///         https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods <br/>
        /// See: [Task.Run Etiquette Examples: Even in the Complex Case, Don't Use Task.Run in the Implementation]
        ///         https://blog.stephencleary.com/2013/11/taskrun-etiquette-examples-even-in.html <br/>
        /// See: [Task.Run Etiquette and Proper Usage]
        ///         https://blog.stephencleary.com/2013/10/taskrun-etiquette-and-proper-usage.html
        /// </summary>
        private async Task RunReceiveMessageLoop() {
            using (var pooledMapper = pool.ObjectMapper.Get()) {
                await ReceiveMessageLoop(pooledMapper.instance).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Currently using a single reused context. This is possible as this loop wait for completion of request execution.
        /// This approach causes <b>head-of-line blocking</b> for each WebSocket client. <br/>
        /// For an <b>out-of-order delivery</b> implementation individual <see cref="SyncContext"/>'s, <see cref="SyncBuffers"/>
        /// and <see cref="MemoryBuffer"/>'s are needed. Heap allocations can be avoided by pooling these instances.
        /// </summary>
        private async Task ReceiveMessageLoop(ObjectMapper mapper) {
            var memoryStream            = new MemoryStream();
            var buffer                  = new ArraySegment<byte>(new byte[8192]);
            var syncPools               = new SyncPools(typeStore);
            var syncBuffers             = new SyncBuffers(new List<SyncRequestTask>(), new List<JsonValue>());
            var syncContext             = new SyncContext(sharedEnv, this, syncBuffers, syncPools); // reused context
            var memoryBuffer            = new MemoryBuffer(4 * 1024);
            // using an instance pool for reading syncRequest and its dependencies is possible as their references
            // are only used within this method scope.
            mapper.reader.InstancePool  = new ReaderInstancePool(typeStore);
            while (true) {
                var state = webSocket.State;
                if (state == WebSocketState.CloseReceived) {
                    var description = webSocket.CloseStatusDescription;
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, description, CancellationToken.None).ConfigureAwait(false);
                    return;
                }
                if (state != WebSocketState.Open) {
                    // Logger.Log(HubLog.Info, $"receive loop finished. WebSocket state: {state}, remote: {remoteEndPoint}");
                    return;
                }
                // --- 1. read message from stream
                memoryStream.Position = 0;
                memoryStream.SetLength(0);
                WebSocketReceiveResult wsResult;
                do {
                    wsResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                    memoryStream.Write(buffer.Array, buffer.Offset, wsResult.Count);
                }
                while(!wsResult.EndOfMessage);
                
                if (wsResult.MessageType != WebSocketMessageType.Text) {
                    continue;
                }
                
                // --- 2. parse and execute message
                var requestContent  = new JsonValue(memoryStream.GetBuffer(), (int)memoryStream.Position);
                syncContext.Init();
                syncContext.SetMemoryBuffer(memoryBuffer);
                mapper.reader.InstancePool?.Reuse();
                
                // inlined ExecuteJsonRequest() to avoid async call:
                // JsonResponse response = await remoteHost.ExecuteJsonRequest(mapper, requestContent, syncContext).ConfigureAwait(false);
                JsonResponse response;
                try {
                    Interlocked.Increment(ref hostMetrics.webSocket.receivedCount);
                    var t1 = Stopwatch.GetTimestamp();
                    var syncRequest = RemoteUtils.ReadSyncRequest(mapper.reader, requestContent, out string error);
                    var t2 = Stopwatch.GetTimestamp();
                    
                    if (error != null) {
                        response = JsonResponse.CreateError(mapper.writer, error, ErrorResponseType.BadResponse, null);
                    } else {
                        var execution   = hub.InitSyncRequest(syncRequest);
                        ExecuteSyncResult syncResult;
                        if (execution == ExecutionType.Sync) {
                            syncResult  =       hub.ExecuteRequest      (syncRequest, syncContext);
                        } else {
                            syncResult  = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false);
                        }
                        response = RemoteHost.CreateJsonResponse(syncResult, syncRequest.reqId, mapper.writer);
                    }
                    var t3 = Stopwatch.GetTimestamp();
                    
                    Interlocked.Add(ref hostMetrics.webSocket.requestReadTime,     t2 - t1);
                    Interlocked.Add(ref hostMetrics.webSocket.requestExecuteTime,  t3 - t2);
                }
                catch (Exception e) {
                    var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                    response = JsonResponse.CreateError(mapper.writer, errorMsg, ErrorResponseType.Exception, null);
                }
                sendQueue.AddTail(response.body); // Enqueue() copy the result.body array
            }
        }
        
        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="Logger"/> <br/>
        /// </summary>
        public static async Task SendReceiveMessages(
            WebSocket   websocket,
            IPEndPoint  remoteEndPoint,
            RemoteHost  remoteHost)
        {
            var  target     = new WebSocketHost(remoteHost, websocket, remoteEndPoint);
            Task sendLoop   = null;
            try {
                sendLoop = target.RunSendMessageLoop();

                await target.RunReceiveMessageLoop().ConfigureAwait(false);

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