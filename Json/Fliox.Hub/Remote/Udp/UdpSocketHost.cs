// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Utils;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Fliox.Hub.Remote.Udp
{
    /// <summary>
    /// Initial implementation based on <see cref="WebSocketHost"/>
    /// </summary>
    public sealed class UdpSocketHost : SocketHost, IDisposable
    {
        private  readonly   UdpClient                           udpClient;
        private  readonly   MessageBufferQueueAsync<VoidMeta>   sendQueue;
        private  readonly   List<JsonValue>                     messages;
        private  readonly   IPEndPoint                          remoteEndPoint;
        private  readonly   HostMetrics                         hostMetrics;


        private UdpSocketHost (
            UdpClient   udpClient,
            IPEndPoint  remoteEndPoint,
            FlioxHub    hub,
            HostEnv     hostEnv)
        : base (hub, hostEnv)
        {
            this.udpClient          = udpClient;
            this.remoteEndPoint     = remoteEndPoint;
            hostMetrics             = hostEnv.metrics;
            sendQueue               = new MessageBufferQueueAsync<VoidMeta>();
            messages                = new List<JsonValue>();
        }
        
        public void Dispose() {
            sendQueue.Dispose();
        }

        // --- IEventReceiver
        public override bool    IsRemoteTarget ()   => true;
        public override bool    IsOpen ()           => true;
        
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
        /// 2. event messages send with <see cref="SocketHost.SendEvent"/>'s <br/>
        /// The loop ensures a UdpClient.SendAsync() is called only once at a time.
        /// </remarks>
        /// <seealso cref="UdpSocketHost.RunReceiveMessageLoop"/>
        private async Task RunSendMessageLoop() {
            try {
                await SendMessageLoop().ConfigureAwait(false);
            } catch (Exception e) {
                var msg = GetExceptionMessage("RunSendMessageLoop()", remoteEndPoint, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        // Send queue (sendWriter / sendReader) is required  to prevent having more than one UdpClient.SendAsync() call outstanding.
        private async Task SendMessageLoop() {
            var buffer = new byte[128];  
            while (true) {
                var remoteEvent = await sendQueue.DequeMessagesAsync(messages).ConfigureAwait(false);
                foreach (var message in messages) {
                    if (LogMessage) {
                        Logger.Log(HubLog.Info, message.AsString());
                    }
                    var length = message.Count;
                    if (buffer.Length < length) {
                        buffer = new byte[length];
                    }
                    message.CopyTo(buffer);
                    // if (sendMessage.Count > 100000) Console.WriteLine($"SendLoop. size: {sendMessage.Count}");
                    await udpClient.SendAsync(buffer, length, remoteEndPoint).ConfigureAwait(false);

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
            while (true) {
                memoryStream.Position = 0;
                memoryStream.SetLength(0);
                
                // --- 1. Read request from datagram
                var receiveResult   = await udpClient.ReceiveAsync().ConfigureAwait(false);
                
                var buffer          = receiveResult.Buffer;
                if (memoryStream.Capacity < buffer.Length) {
                    memoryStream.Capacity = buffer.Length;
                }
                memoryStream.Write(buffer, 0, buffer.Length);

                var request = new JsonValue(memoryStream.GetBuffer(), (int)memoryStream.Position);
                try {
                    // --- 2. Parse request
                    Interlocked.Increment(ref hostMetrics.udp.receivedCount);
                    var t1          = Stopwatch.GetTimestamp();
                    var syncRequest = ParseRequest(request);
                    var t2          = Stopwatch.GetTimestamp();
                    Interlocked.Add(ref hostMetrics.udp.requestReadTime, t2 - t1);
                    if (syncRequest == null) {
                        continue;
                    }
                    // --- 3. Execute request
                    ExecuteRequest (syncRequest);
                    var t3          = Stopwatch.GetTimestamp();
                    Interlocked.Add(ref hostMetrics.udp.requestExecuteTime, t3 - t2);
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
            UdpClient   udpClient,
            IPEndPoint  remoteEndPoint,
            FlioxHub    hub,
            HostEnv     hostEnv)
        {
            var  target     = new UdpSocketHost(udpClient, remoteEndPoint, hub, hostEnv);
            Task sendLoop   = null;
            try {
                sendLoop = target.RunSendMessageLoop();

                await target.RunReceiveMessageLoop().ConfigureAwait(false);

                target.sendQueue.Close();
            }
            catch (SocketException e) {
                var msg = GetExceptionMessage("UdpSocketHost.SendReceiveMessages()", remoteEndPoint, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("UdpSocketHost.SendReceiveMessages()", remoteEndPoint, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            finally {
                if (sendLoop != null) {
                    await sendLoop.ConfigureAwait(false);
                }
                target.Dispose();
                udpClient.Dispose();
            }
        }
        
        private static string GetExceptionMessage(string location, IPEndPoint remoteEndPoint, Exception e) {
            if (e.InnerException is HttpListenerException listenerException) {
                e = listenerException;
                // observed ErrorCode:
                // 995 The I/O operation has been aborted because of either a thread exit or an application request.
                return $"{location} {e.GetType().Name}: {e.Message} ErrorCode: {listenerException.ErrorCode}, remote: {remoteEndPoint} ";
            }
            if (e is SocketException wsException) {
                return $"{location} {e.GetType().Name} {e.Message} ErrorCode: {wsException.ErrorCode}, HResult: 0x{e.HResult:X}, remote: {remoteEndPoint}";
            }
            return $"{location} {e.GetType().Name}: {e.Message}, remote: {remoteEndPoint}";
        }
    }
}