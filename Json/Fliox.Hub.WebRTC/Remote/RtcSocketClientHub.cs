// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Hub.WebRTC.Impl;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    /// <summary>
    /// Store send requests in the <see cref="requestMap"/> to map received response messages to its related <see cref="SyncRequest"/>
    /// </summary>
    internal sealed class WebRtcConnection
    {
        internal  readonly  DataChannel         dc;
        internal  readonly  RemoteRequestMap    requestMap;
        
        internal WebRtcConnection(DataChannel dataChannel) {
            dc          = dataChannel;
            requestMap  = new RemoteRequestMap();
        }
    }

    public sealed class RtcSocketClientHub : SocketClientHub
    {
        private  readonly   WebRtcConfig                config;
        private  readonly   string                      remoteHost;
        private  readonly   string                      remoteHostId;
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public   override   bool                        IsConnected => rtcConnection?.dc.ReadyState == DataChannelState.open;
        private  readonly   ObjectReader                reader;
        private  readonly   Signaling                   signaling;

        private  readonly   object                      connectLock = new object();
        private             Task<WebRtcConnection>      connectTask;
        private             PeerConnection              pc;
        private             WebRtcConnection            rtcConnection;
        
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        
        public   override   string                      ToString() => $"{database.name} - config: {config}";
        
        public RtcSocketClientHub(
            string              dbName,
            string              remoteHost,
            WebRtcConfig        config,
            SharedEnv           env = null,
            RemoteClientAccess  access = RemoteClientAccess.Single)
            : base(new RemoteDatabase(dbName), env, 0, access)
        {
            this.remoteHost     = remoteHost;
            var uri             = new Uri(remoteHost);
            var query           = HttpUtility.ParseQueryString(uri.Query);
            remoteHostId        = query.Get("host");
            var signalingSocket = new WebSocketClientHub("signaling", remoteHost, env, RemoteClientAccess.Single);
            signaling           = new Signaling(signalingSocket) { UserId = "admin", Token = "admin" };
            var mapper          = new ObjectMapper(sharedEnv.TypeStore);
            reader              = mapper.reader;
            this.config         = config;
        }
        
        private Task<WebRtcConnection> JoinConnects(out TaskCompletionSource<WebRtcConnection> tcs, out WebRtcConnection connection) {
            lock (connectLock) {
                if (connectTask != null) {
                    connection  = null;
                    tcs         = null;
                    return connectTask;
                }
                connection  = rtcConnection;
                tcs         = new TaskCompletionSource<WebRtcConnection>();
                connectTask = tcs.Task;
                return connectTask;
            }
        }
        
        private async Task<WebRtcConnection> Connect() {
            var task = JoinConnects(out var tcs, out WebRtcConnection connection);
            if (tcs == null) {
                connection = await task.ConfigureAwait(false);
                return connection;
            }
            try {
                await ConnectToRtcHost().ConfigureAwait(false);
                
                connectTask = null;
                tcs.SetResult(connection);
            }
            catch (Exception e) {
                connectTask = null;
                tcs.SetException(e);
                throw;
            }
            return rtcConnection;
        }
        
        private async Task ConnectToRtcHost()
        {
            // subscription cause assigning client id by server
            signaling.SubscribeMessage<HostIce>(nameof(HostIce), (message, context) => {
                if (!message.GetParam(out var value, out var hostIceError)) {
                    Logger.Log(HubLog.Error, $"invalid host ICE candidate. error: {hostIceError}");
                    return;
                }
                var parseCandidate = IceCandidate.TryParse(value.candidate.AsString(), out var iceCandidate);
                if (!parseCandidate) {
                    Logger.Log(HubLog.Error, "invalid ICE candidate"); // TODO why TryParse() return false
                }
                pc.AddIceCandidate(iceCandidate);
            });
            await signaling.SyncTasks().ConfigureAwait(false);
            
            if (signaling.UserInfo.clientId.IsNull()) throw new InvalidOperationException("expect client id not null");
            
            // --- create offer SDP
            pc              = new PeerConnection(config);
            var dc          = await pc.CreateDataChannel("test").ConfigureAwait(false); // right after connection creation. Otherwise: NoRemoteMedia
            
            var changeOpened = new TaskCompletionSource<bool>();
            dc.OnOpen    += ()      => {
                Logger.Log(HubLog.Info, "datachannel onopen");
                changeOpened.SetResult(true);
            };
            dc.OnMessage += (data)      => OnMessage(data); 
            dc.OnClose   += ()          => { Logger.Log(HubLog.Info, "datachannel onclose"); };
            dc.OnError   += dcError     => { Logger.Log(HubLog.Error, $"datachannel onerror: {dcError}"); };
            
            pc.OnIceCandidate += candidate => {
                // is called on separate thread
                var jsonCandidate   = new JsonValue(candidate.ToJson());
                var iceCandidate    = new ClientIce { candidate = jsonCandidate };
                // send ICE candidate to WebRTC Host
                var msg             = signaling.SendMessage(nameof(ClientIce), iceCandidate);
                msg.EventTargetClient(signaling.ClientId);
                _ = signaling.SyncTasks();
            };
            pc.OnConnectionStateChange += state => {
                Logger.Log(HubLog.Info, $"on WebRTC client connection state change: {state}");
            };
            var offer = await pc.CreateOffer().ConfigureAwait(false);  // fire onicecandidate
            await pc.SetLocalDescription(offer).ConfigureAwait(false);

            // --- send offer SDP -> Signaling Server -> WebRTC Host
            var connectResult   = signaling.ConnectClient(new ConnectClient { hostId = remoteHostId, offerSDP = offer.sdp });
            await signaling.SyncTasks().ConfigureAwait(false);
            
            var result              = connectResult.Result;

            var answerDescription   = new SessionDescription { type = SdpType.answer, sdp = result.answerSDP };
            var descError = await pc.SetRemoteDescription(answerDescription).ConfigureAwait(false);
            if (descError != null) {
                throw new InvalidOperationException($"setRemoteDescription failed. error: {descError}");
            }
            rtcConnection = new WebRtcConnection(dc);
            
            await changeOpened.Task.ConfigureAwait(false);
        }
        
        public override Task Close() {
            rtcConnection.dc.Close();
            rtcConnection = null;
            return Task.CompletedTask;
        }
        
        /* public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        } */

        private void OnMessage(byte[] data) {
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
                    var sendBuffer  = rawRequest.AsByteArray();
                    if (env.logMessages) TransportUtils.LogMessage(Logger, ref sbSend, "client  ->", remoteHost, rawRequest);
                    // --- Send message
                    conn.dc.Send(sendBuffer); // requires byte[] an individual byte[] :(
                    
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
