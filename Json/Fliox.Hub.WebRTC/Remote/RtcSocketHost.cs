// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
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
    internal sealed class RtcSocketHost : SocketHost, IDisposable
    {
        internal readonly   RTCPeerConnection                   pc;
        internal            RTCDataChannel                      remoteDc;
        private  readonly   MessageBufferQueueAsync<VoidMeta>   sendQueue;
        private  readonly   List<JsonValue>                     messages;
        private  readonly   string                              remoteClient;
        private  readonly   RemoteHostEnv                       hostEnv;
        private             StringBuilder                       sbSend;
        private             StringBuilder                       sbRecv;

        internal RtcSocketHost (
            RTCPeerConnection   peerConnection,
            string              remoteClient,
            FlioxHub            hub,
            IHost               host)
        : base (hub, host)
        {
            hostEnv             = hub.GetFeature<RemoteHostEnv>();
            this.remoteClient   = remoteClient;
            sendQueue           = new MessageBufferQueueAsync<VoidMeta>();
            messages            = new List<JsonValue>();
            pc                  = peerConnection;
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
            return remoteDc.readyState == RTCDataChannelState.open;
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
                    var array = message.AsByteArray();
                    // if (sendMessage.Count > 100000) Console.WriteLine($"SendLoop. size: {sendMessage.Count}");
                    remoteDc.send(array); // requires byte[] an individual byte[] :(
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        internal void OnMessage(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data) {
            var request = new JsonValue(data);
            if (hostEnv.logMessages) LogMessage(Logger, ref sbRecv, " server <-", remoteClient, request);
            OnReceive(request, ref hostEnv.metrics.webSocket);
        }
        
        internal async Task SendReceiveMessages()
        {
            try {
                await RunSendMessageLoop().ConfigureAwait(false);

                sendQueue.Close();
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("WebSocketHost.SendReceiveMessages()", remoteClient, e);
                Logger.Log(HubLog.Info, msg);
            }
            finally {
                Dispose();
                remoteDc.close();
            }
        }
        
        private static string GetExceptionMessage(string location, string remoteEndPoint, Exception e) {
            return $"{location} {e.GetType().Name}: {e.Message}, remote: {remoteEndPoint}";
        }
    }
}

#endif