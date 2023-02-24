// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Remote.TransportUtils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Remote
{
    public sealed class WebRtcHost : SocketHost, IDisposable
    {
        private  readonly   WebSocket                           webSocket;
        private  readonly   MessageBufferQueueAsync<VoidMeta>   sendQueue;
        private  readonly   List<JsonValue>                     messages;
        private  readonly   IPEndPoint                          remoteClient;
        private  readonly   RemoteHostEnv                       hostEnv;
        private             StringBuilder                       sbSend;
        private             StringBuilder                       sbRecv;

        private WebRtcHost (
            WebSocket       webSocket,
            IPEndPoint      remoteClient,
            FlioxHub        hub,
            IHost           host)
        : base (hub, host)
        {
            hostEnv                 = hub.GetFeature<RemoteHostEnv>();
            this.webSocket          = webSocket;
            this.remoteClient       = remoteClient;
            sendQueue               = new MessageBufferQueueAsync<VoidMeta>();
            messages                = new List<JsonValue>();
        }
        
        public void Dispose() {
            sendQueue.Dispose();
        }

        // --- IEventReceiver
        protected override string  Endpoint            => $"ws:{remoteClient}";
        protected override bool    IsRemoteTarget ()   => true;
        protected override bool    IsOpen () {
            if (hostEnv.fakeOpenClosedSockets)
                return true;
            return webSocket.State == WebSocketState.Open;
        }
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message) {
            if (sendQueue.Closed)
                return;
            sendQueue.AddTail(message);
        }
        
        /// <summary>
        /// Loop is purely I/O bound => don't wrap in
        /// return Task.Run(async () => { ... });
        /// </summary>
        /// <remarks>
        /// A send loop reading from a queue is required as message can be sent from two different sources <br/>
        /// 1. response messages created in <see cref="ReceiveMessageLoop"/> <br/>
        /// 2. event messages send with <see cref="SocketHost.SendEvent"/>'s <br/>
        /// The loop ensures a WebSocket.SendAsync() is called only once at a time.
        /// </remarks>
        /// <seealso cref="WebSocketHost.RunReceiveMessageLoop"/>
        private async Task RunSendMessageLoop() {
            try {
                await SendMessageLoop().ConfigureAwait(false);
            } catch (Exception e) {
                var msg = GetExceptionMessage("RunSendMessageLoop()", remoteClient, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        /// Send queue is required to ensure having only a single outstanding SendAsync() at any time
        // Otherwise:
        // System.InvalidOperationException: There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time. 
        private async Task SendMessageLoop() {
            while (true) {
                var remoteEvent = await sendQueue.DequeMessageValuesAsync(messages).ConfigureAwait(false);
                foreach (var message in messages) {
                    if (hostEnv.logMessages) LogMessage(Logger, ref sbSend, " server ->", remoteClient, message);
                    var arraySegment = message.AsReadOnlyMemory();
                    // if (sendMessage.Count > 100000) Console.WriteLine($"SendLoop. size: {sendMessage.Count}");
                    await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
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
                if (hostEnv.logMessages) LogMessage(Logger, ref sbRecv, " server <-", remoteClient, request);
                OnReceive(request, ref hostEnv.metrics.webSocket);
            }
        }
        
        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="FlioxHub.Logger"/> <br/>
        /// </summary>
        public static async Task SendReceiveMessages(
            WebSocket       websocket,
            IPEndPoint      remoteClient,
            HttpHost        host)
        {
            var hub         = host.hub; 
            var  target     = new WebRtcHost(websocket, remoteClient, hub, host);
            Task sendLoop   = null;
            try {
                sendLoop = target.RunSendMessageLoop();

                await target.RunReceiveMessageLoop().ConfigureAwait(false);

                target.sendQueue.Close();
            }
            catch (WebSocketException e) {
                var msg = GetExceptionMessage("WebSocketHost.SendReceiveMessages()", remoteClient, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("WebSocketHost.SendReceiveMessages()", remoteClient, e);
                hub.Logger.Log(HubLog.Info, msg);
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

#endif