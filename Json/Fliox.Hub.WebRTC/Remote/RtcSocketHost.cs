// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Utils;
using SIPSorcery.Net;
using static Friflo.Json.Fliox.Hub.Remote.TransportUtils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class RtcSocketHost : SocketHost, IDisposable
    {
        internal readonly   RTCPeerConnection                   connection;
        private             RTCDataChannel                      channel;
        private  readonly   MessageBufferQueueAsync<VoidMeta>   sendQueue;
        private  readonly   List<JsonValue>                     messages;
        private  readonly   string                              remoteClient;
        private  readonly   RemoteHostEnv                       hostEnv;
        private             StringBuilder                       sbSend;
        private             StringBuilder                       sbRecv;

        internal RtcSocketHost (
            RTCConfiguration    config,
            string              remoteClient,
            FlioxHub            hub,
            IHost               host)
        : base (hub, host)
        {
            hostEnv             = hub.GetFeature<RemoteHostEnv>();
            this.remoteClient   = remoteClient;
            sendQueue           = new MessageBufferQueueAsync<VoidMeta>();
            messages            = new List<JsonValue>();
            connection          = new RTCPeerConnection(config);
            connection.onconnectionstatechange += (state) => {
                Logger.Log(HubLog.Info, $"on WebRTC host connection state change: {state}");
            };
            connection.ondatachannel += (rdc) => {
                channel = rdc;
                channel.onmessage += OnMessage;
            };
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
            return channel.readyState == RTCDataChannelState.open;
        }
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message) {
            if (sendQueue.Closed)
                return;
            sendQueue.AddTail(message);
        }
        
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
                    var array = message.MutableArray;
                    // if (sendMessage.Count > 100000) Console.WriteLine($"SendLoop. size: {sendMessage.Count}");
                    channel.send(array);
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        private void OnMessage(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data) {
            var request = new JsonValue(data);
            if (hostEnv.logMessages) LogMessage(Logger, ref sbRecv, " server <-", remoteClient, request);
            OnReceive(request, ref hostEnv.metrics.webSocket);
        }
        
        internal async Task SendReceiveMessages()
        {
            Task sendLoop   = null;
            try {
                sendLoop = RunSendMessageLoop();

                sendQueue.Close();
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("WebSocketHost.SendReceiveMessages()", remoteClient, e);
                Logger.Log(HubLog.Info, msg);
            }
            finally {
                if (sendLoop != null) {
                    await sendLoop.ConfigureAwait(false);
                }
                Dispose();
                channel.close();
            }
        }
        
        private static string GetExceptionMessage(string location, string remoteEndPoint, Exception e) {
            if (e.InnerException is HttpListenerException listenerException) {
                e = listenerException;
                // observed ErrorCode:
                // 995 The I/O operation has been aborted because of either a thread exit or an application request.
                return $"{location} {e.GetType().Name}: {e.Message} ErrorCode: {listenerException.ErrorCode}, remote: {remoteEndPoint} ";
            }
            return $"{location} {e.GetType().Name}: {e.Message}, remote: {remoteEndPoint}";
        }
    }
}

#endif