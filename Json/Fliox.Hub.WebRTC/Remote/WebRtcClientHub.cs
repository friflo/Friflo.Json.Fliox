// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;
using SIPSorcery.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Remote
{
    /// <summary>
    /// Store send requests in the <see cref="requestMap"/> to map received response messages to its related <see cref="SyncRequest"/>
    /// </summary>
    internal sealed class WebRtcConnection
    {
        internal  readonly  RTCDataChannel      channel;
        internal  readonly  RemoteRequestMap    requestMap;
        
        internal WebRtcConnection(RTCDataChannel channel) {
            this.channel    = channel;
            requestMap      = new RemoteRequestMap();
        }
    }

    public  sealed class WebRtcClientHub : SocketClientHub
    {
        private  readonly   RTCConfiguration            config;
        private  readonly   string                      remoteHost = "---";                  
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public   override   bool                        IsConnected => rtcConnection?.channel.readyState == RTCDataChannelState.open;
        private  readonly   ObjectReader                reader;

        private             WebRtcConnection                        rtcConnection;
        private             TaskCompletionSource<WebRtcConnection>  connected;
        
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        
        public   override   string                      ToString() => $"{database.name} - config: {config}";
        
        public WebRtcClientHub(string dbName, RTCConfiguration config, SharedEnv env = null, RemoteClientAccess access = RemoteClientAccess.Single)
            : base(new RemoteDatabase(dbName), env, 0, access)
        {
            var rtcPeerConnection = new RTCPeerConnection(config);
            rtcPeerConnection.ondatachannel += (dc) => {
                rtcConnection = new WebRtcConnection(dc);
                dc.onmessage += OnMessage;
                connected?.SetResult(rtcConnection);
            };
            var mapper = new ObjectMapper(sharedEnv.TypeStore);
            reader = mapper.reader;
            this.config = config;
        }
        
        public override Task Close() {
            rtcConnection.channel.close();
            return Task.CompletedTask;
        }
        
        /* public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        } */
        
        private Task<WebRtcConnection> Connect() {
            connected ??= new TaskCompletionSource<WebRtcConnection>();
            return connected.Task;
        }
        
        private void OnMessage(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data) {
            var message     = new JsonValue(data);
            // --- process received message
            if (env.logMessages) TransportUtils.LogMessage(Logger, ref sbRecv, "client  <-", remoteHost, message);
            OnReceive(message, rtcConnection.requestMap, reader);
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            var conn = rtcConnection;
            if (conn == null) {
                conn = await Connect().ConfigureAwait(false);
            }
            int sendReqId       = Interlocked.Increment(ref reqId);
            syncRequest.reqId   = sendReqId;
            try {
                // requires its own mapper - method can be called from multiple threads simultaneously
                using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                    var writer      = MessageUtils.GetCompactWriter(pooledMapper.instance);
                    var rawRequest  = MessageUtils.WriteProtocolMessage(syncRequest, sharedEnv, writer);
                    // request need to be queued _before_ sending it to be prepared for handling the response.
                    var request     = new RemoteRequest(syncContext, cancellationToken);
                    conn.requestMap.Add(sendReqId, request);
                    var sendBuffer  = rawRequest.MutableArray;
                    if (env.logMessages) TransportUtils.LogMessage(Logger, ref sbSend, "client  ->", remoteHost, rawRequest);
                    // --- Send message
                    conn.channel.send(sendBuffer);
                    
                    // --- Wait for response
                    var response = await request.response.Task.ConfigureAwait(false);
                    
                    return CreateSyncResult(response);
                }
            }
            catch (Exception e) {
                return CreateSyncError(e, remoteHost);
            }
        }
    }
}

#endif