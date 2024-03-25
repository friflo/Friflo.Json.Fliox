// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        internal            DataChannel         dc;
        internal  readonly  RemoteRequestMap    requestMap;
        
        internal WebRtcConnection() {
            requestMap  = new RemoteRequestMap();
        }
    }
    
    internal sealed class ConnectCredentials
    {
        internal  readonly  ShortString     userId; 
        internal  readonly  ShortString     token;
        
        internal ConnectCredentials(in ShortString userId, in ShortString token) {
            this.userId = userId;
            this.token  = token;
        }
    }

    public sealed class RtcSocketClientHub : SocketClientHub
    {
        private  readonly   WebRtcConfig                config;
        private  readonly   string                      signalingHost;
        private  readonly   string                      remoteHostId;
        private             string                      hostClientId;
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public   override   bool                        IsConnected => rtcConnection?.dc.ReadyState == DataChannelState.open;
        private  readonly   ObjectReader                reader;
        private  readonly   Signaling                   signaling;
        private             ShortString                 ClientId => signaling.UserInfo.clientId;

        private  readonly   object                      connectLock = new object();
        private             Task<WebRtcConnection>      connectTask;
        private             PeerConnection              pc;
        private             WebRtcConnection            rtcConnection;
        private  readonly   List<ClientIce>             localIceCandidates  = new List<ClientIce>(); 
        private  readonly   List<IceCandidate>          iceCandidates       = new List<IceCandidate>(); 
        
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        
        public   override   string                      ToString() => $"{database.name} - host: {signalingHost}";
        
        public RtcSocketClientHub(
            string              dbName,
            string              signalingHost,
            WebRtcConfig        config,
            SharedEnv           env = null,
            RemoteClientAccess  access = RemoteClientAccess.Single)
            : base(new RemoteDatabase(dbName), env, 0, access)
        {
            this.signalingHost  = signalingHost;
            var uri             = new Uri(signalingHost);
            var query           = HttpUtility.ParseQueryString(uri.Query);
            remoteHostId        = query.Get("host") ?? throw new ArgumentException($"missing host parameter in signalingHost: {signalingHost}");
            var signalingDB     = query.Get("db")   ?? "signaling";
            var signalingSocket = new WebSocketClientHub(signalingDB, signalingHost, env, RemoteClientAccess.Single);
            signaling           = new Signaling(signalingSocket); // user / token assigned on connect
            var mapper          = new ObjectMapper(sharedEnv.TypeStore);
            reader              = mapper.reader;
            this.config         = config;
            LogInfo             = message => Logger.Log(HubLog.Info, $"RTC-client({ClientId}): {message}");
        }
        
        private event Action<string> LogInfo;
        private event Action<string> LogNull; 
        
        private void LogError(string message, Exception exception = null) {
            Logger.Log(HubLog.Error, $"RTC-client({ClientId}): {message}", exception);
        }

        private Task<WebRtcConnection> JoinConnects(out TaskCompletionSource<WebRtcConnection> tcs, out WebRtcConnection connection) {
            lock (connectLock) {
                if (connectTask != null) {
                    connection  = null;
                    tcs         = null;
                    return connectTask;
                }
                connection  = rtcConnection = new WebRtcConnection();
                tcs         = new TaskCompletionSource<WebRtcConnection>();
                connectTask = tcs.Task;
                return connectTask;
            }
        }
        
        private async Task<WebRtcConnection> Connect(ConnectCredentials credentials) {
            var task = JoinConnects(out var tcs, out WebRtcConnection connection);
            if (tcs == null) {
                connection = await task.ConfigureAwait(false);
                return connection;
            }
            try {
                await ConnectToRtcHost(credentials).ConfigureAwait(false);
                
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
        
        private async Task ConnectToRtcHost(ConnectCredentials credentials)
        {
            LogInfo?.Invoke("--- ConnectToRtcHost() start");
            // subscription cause assigning client id by server
            signaling.UserId    = credentials.userId.AsString();
            signaling.Token     = credentials.token.AsString();
            signaling.SubscribeMessage<Answer>(nameof(Answer), async (message, context) => {
                if (!message.GetParam(out var answer, out var answerError)) {
                    LogError($"invalid Answer message. error: {answerError}");
                    return;
                }
                LogInfo?.Invoke($"received answer. client: {answer.client}");
                if (!answer.client.IsEqual(ClientId)) {
                    LogError($"received answer with invalid client: {answer.client}");
                    return;
                }
                LogInfo?.Invoke("SetRemoteDescription");
                var answerDescription   = new SessionDescription { type = SdpType.answer, sdp = answer.sdp };
                var descError           = await pc.SetRemoteDescription(answerDescription).ConfigureAwait(false);
                if (descError != null) {
                    throw new InvalidOperationException($"setRemoteDescription failed. error: {descError}");
                }
                foreach (var candidate in iceCandidates) {
                    LogNull?.Invoke("--- AddIceCandidate");
                    pc.AddIceCandidate(candidate);
                }
                iceCandidates.Clear();
            });
            signaling.SubscribeMessage<HostIce>(nameof(HostIce), (message, context) => {
                if (!message.GetParam(out var ice, out var hostIceError)) {
                    LogError($"invalid HostIce message. error: {hostIceError}");
                    return;
                }
                LogNull?.Invoke($"received ICE candidate. client: {ice.client}");
                if (!ice.client.IsEqual(ClientId)) {
                    LogError($"received ICE candidate with invalid client: {ice.client}");
                    return;
                }
                var parseCandidate = IceCandidate.TryParse(ice.candidate.AsString(), out var iceCandidate);
                if (!parseCandidate) {
                    LogError("invalid ICE candidate"); // TODO why TryParse() return false
                    return;
                }
                var signalingState = pc.SignalingState;
                if (signalingState == SignalingState.HaveRemotePrAnswer || signalingState == SignalingState.Stable) {
                    LogNull?.Invoke("--- AddIceCandidate");
                    pc.AddIceCandidate(iceCandidate);
                } else {
                    iceCandidates.Add(iceCandidate);
                }
            });
            await signaling.SyncTasks().ConfigureAwait(false);
            
            if (ClientId.IsNull()) throw new InvalidOperationException("expect client id not null");
            
            // --- create offer SDP
            pc              = new PeerConnection(config);
            var dc          = await pc.CreateDataChannel("test").ConfigureAwait(false); // right after connection creation. Otherwise: NoRemoteMedia
            
            var channelOpen     = new TaskCompletionSource<bool>();
            dc.OnOpen    += ()      => {
                LogInfo?.Invoke("data channel onopen");
                if (dc.ReadyState != DataChannelState.open) { LogError("expect ReadyState==open"); }
                channelOpen.SetResult(true);
            };
            dc.OnMessage += (data)      => OnMessage(data);
            dc.OnClose   += ()          => { LogInfo?.Invoke("data channel closed"); };
            dc.OnError   += dcError     => { LogError($"data channel error: {dcError}"); };
            
            pc.OnIceCandidate += async (candidate) => {
                // is called on separate thread
                var jsonCandidate   = new JsonValue(candidate.ToJson());
                var iceCandidate    = new ClientIce { client = ClientId, candidate = jsonCandidate };
                // send ICE candidate -> Signaling Server -> WebRTC Host
                if (hostClientId == null) {
                    localIceCandidates.Add(iceCandidate);                    
                } else {
                    var msg             = signaling.SendMessage(nameof(ClientIce), iceCandidate);
                    msg.EventTargetClient(hostClientId);
                    _ = signaling.SyncTasks().ConfigureAwait(false);
                }
            };
            pc.OnConnectionStateChange += state => {
                LogInfo?.Invoke($"connection state change: {state}");
            };
            var offer = await pc.CreateOffer().ConfigureAwait(false);  // fire OnIceCandidate
            await pc.SetLocalDescription(offer).ConfigureAwait(false);

            // --- send offer SDP -> Signaling Server -> WebRTC Host
            var connectResult   = signaling.ConnectClient(new ConnectClient { hostId = remoteHostId, offerSDP = offer.sdp });
            await signaling.SyncTasks().ConfigureAwait(false);
            
            if (!connectResult.Success) {
                Logger.Log(HubLog.Error, $"ConnectClient failed. error: {connectResult.Error}");
                return;
            };
            hostClientId = connectResult.Result.hostClientId;
            foreach (var localIceCandidate in localIceCandidates) {
                var msg = signaling.SendMessage(nameof(ClientIce), localIceCandidate);
                msg.EventTargetClient(hostClientId);
                _ = signaling.SyncTasks().ConfigureAwait(false);
            }
            LogInfo?.Invoke("--- ConnectToRtcHost() wait");
            await channelOpen.Task.ConfigureAwait(false);   // continues on WebRTC callback thread 
            await Task.Delay(1).ConfigureAwait(false);      // move execution to ThreadPool Worker

            LogInfo?.Invoke("--- ConnectToRtcHost() finished");
            rtcConnection.dc = dc;
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
            // LogInfo?.Invoke($"received message: {message}");
            // --- process received message
            if (env.logMessages) TransportUtils.LogMessage(Logger, ref sbRecv, "client  <-", signalingHost, message);
            OnReceive(message, rtcConnection.requestMap, reader);
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            var conn = rtcConnection;
            if (conn == null) {
                var cred    = new ConnectCredentials (syncRequest.userId, syncRequest.token);
                conn        = await Connect(cred).ConfigureAwait(false);
                Logger.Log(HubLog.Info, "ExecuteRequestAsync connected");
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
                    if (env.logMessages) TransportUtils.LogMessage(Logger, ref sbSend, "client  ->", signalingHost, rawRequest);
                    // --- Send message
                    conn.dc.Send(sendBuffer, rawRequest.start, rawRequest.Count);
                    
                    // --- Wait for response
                    var response = await request.response.Task.ConfigureAwait(false);
                    
                    return CreateSyncResult(response);
                }
            }
            catch (Exception e) {
                return CreateSyncError(e, signalingHost);
            }
        }
    }
}
