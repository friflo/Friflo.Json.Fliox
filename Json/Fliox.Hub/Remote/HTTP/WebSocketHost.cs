// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Utils;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    // [Things I Wish Someone Told Me About ASP.NET Core WebSockets | codetinkerer.com] https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
    public sealed class WebSocketHost : WebHostHandler, IDisposable
    {
        private  readonly   WebSocket                           webSocket;
        /// Only set to true for testing. It avoids an early out at <see cref="EventSubClient.SendEvents"/> 
        private  readonly   bool                                fakeOpenClosedSocket;
        private  readonly   MessageBufferQueueAsync<VoidMeta>   sendQueue;
        private  readonly   List<JsonValue>                     messages;
        private  readonly   IPEndPoint                          remoteEndPoint;
        private  readonly   HostMetrics                         hostMetrics;


        private WebSocketHost (
            RemoteHost  remoteHost,
            WebSocket   webSocket,
            IPEndPoint  remoteEndPoint)
            : base (remoteHost)
        {
            this.webSocket          = webSocket;
            this.remoteEndPoint     = remoteEndPoint;
            fakeOpenClosedSocket    = remoteHost.fakeOpenClosedSockets;
            hostMetrics             = remoteHost.metrics;
            sendQueue               = new MessageBufferQueueAsync<VoidMeta>();
            messages                = new List<JsonValue>();
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
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message) {
            sendQueue.AddTail(message);
        }

        
        // private  static readonly   Regex   RegExLineFeed   = new Regex(@"\s+");
        private     static readonly   bool    LogMessage      = false;
        
        /// <summary>
        /// Loop is purely I/O bound => don't wrap in
        /// return Task.Run(async () => { ... });
        /// </summary>
        /// <remarks>
        /// A send loop reading from a queue is required as message can be sent from two different sources <br/>
        /// 1. response messages created in <see cref="ReceiveMessageLoop"/> <br/>
        /// 2. event messages send with <see cref="WebHostHandler.SendEvent"/>'s <br/>
        /// The loop ensures a WebSocket.SendAsync() is called only once at a time.
        /// </remarks>
        /// <seealso cref="WebSocketHost.RunReceiveMessageLoop"/>
        private async Task RunSendMessageLoop() {
            try {
                await SendMessageLoop().ConfigureAwait(false);
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
                        Logger.Log(HubLog.Info, message.AsString());
                    }
                    var arraySegment = message.AsReadOnlyMemory();
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
            await ReceiveMessageLoop().ConfigureAwait(false);
        }
        
        /// <summary>
        /// Parse, execute and send response message for all received request messages.<br/>
        /// </summary>
        private async Task ReceiveMessageLoop() {
            var memoryStream    = new MemoryStream();
            var buffer          = new ArraySegment<byte>(new byte[8192]);
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
                // --- 1. Read request from stream
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
                var request = new JsonValue(memoryStream.GetBuffer(), (int)memoryStream.Position);
                try {
                    // --- 2. Parse request
                    Interlocked.Increment(ref hostMetrics.webSocket.receivedCount);
                    var t1          = Stopwatch.GetTimestamp();
                    var syncRequest = ParseRequest(request);
                    var t2          = Stopwatch.GetTimestamp();
                    Interlocked.Add(ref hostMetrics.webSocket.requestReadTime, t2 - t1);
                    if (syncRequest == null) {
                        continue;
                    }
                    // --- 3. Execute request
                    ExecuteRequest (syncRequest);
                    var t3          = Stopwatch.GetTimestamp();
                    Interlocked.Add(ref hostMetrics.webSocket.requestExecuteTime, t3 - t2);
                }
                catch (Exception e) {
                    SendResponseException(e, null);
                }
            }
        }
        
        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="RemoteHost.Logger"/> <br/>
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