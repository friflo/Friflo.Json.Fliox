// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.WebRTC.Impl;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class RtcHostConfig {
        public  string          SignalingDB     { get; init; } = "signaling";
        public  string          SignalingHost   { get; init; }
        public  string          User            { get; init; }
        public  string          Token           { get; init; }
        public  WebRtcConfig    WebRtcConfig    { get; init; }
    }
    
    public sealed class RtcServer : IHost
    {
        private readonly WebRtcConfig                           config;
        private readonly Signaling                              signaling;
        private readonly Dictionary<ShortString, RtcSocketHost> clients;
        private readonly IHubLogger                             logger;
        
        public RtcServer (RtcHostConfig rtcConfig, SharedEnv env = null) {
            config          = rtcConfig.WebRtcConfig;
            clients         = new Dictionary<ShortString, RtcSocketHost>(ShortString.Equality);
            var signalingHub= new WebSocketClientHub (rtcConfig.SignalingDB, rtcConfig.SignalingHost, env);
            signaling       = new Signaling(signalingHub) { UserId = rtcConfig.User, Token = rtcConfig.Token };
            logger          = signaling.Logger;
            logger.Log(HubLog.Info, $"RtcServer listening at: {rtcConfig.SignalingHost} db: '{rtcConfig.SignalingDB}'");
        }

        public async Task AddHost(string hostId, HttpHost host)
        {
            logger.Log(HubLog.Info, $"RtcServer add host: '{hostId}'");
            signaling.SubscribeMessage<Offer>(nameof(Offer), (message, context) => {
                ProcessOffer(host, message);
            });
            signaling.SubscribeMessage<ClientIce>(nameof(ClientIce), (message, context) => {
                ProcessIceCandidate(message, context);
            });
            signaling.AddHost(new AddHost { hostId = hostId });
            await signaling.SyncTasks().ConfigureAwait(false);
        }
        
        private async void ProcessOffer(HttpHost host, Message<Offer> message) {
            if (!message.GetParam(out var offer, out var error)) {
                logger.Log(HubLog.Error, $"invalid Offer. error: {error}");
                return;
            }
            var pc          = new PeerConnection(config);
            var socketHost  = new RtcSocketHost(pc, offer.client.ToString(), host.hub, this);
            clients.Add(offer.client, socketHost);
            
            // --- add peer connection event callbacks
            pc.OnConnectionStateChange += (state) => {
                logger.Log(HubLog.Info, $"on WebRTC host connection state change: {state}");
            };
            pc.OnDataChannel += (remoteDc) => {
                socketHost.remoteDc = remoteDc; // note: remoteDc != dc created bellow
                remoteDc.OnMessage += (data) => socketHost.OnMessage(data);
            };
            pc.OnIceCandidate += candidate => {
                // send ICE candidate to WebRTC client
                var jsonCandidate   = new JsonValue(candidate.ToJson());
                var msg             = signaling.SendMessage(nameof(HostIce), new HostIce { candidate = jsonCandidate });
                msg.EventTargetClient(offer.client);
                _ = signaling.SyncTasks();
            };
            var dc = await pc.CreateDataChannel("test").ConfigureAwait(false); // right after connection creation. Otherwise: NoRemoteMedia
            
            dc.OnError += dcError   => { logger.Log(HubLog.Error, $"datachannel onerror: {dcError}"); };

            var rtcOffer = new SessionDescription { type = SdpType.offer, sdp = offer.sdp };
            if (!await pc.SetRemoteDescription(rtcOffer, out var descError).ConfigureAwait(false)) {
                logger.Log(HubLog.Error, $"setRemoteDescription failed. error: {descError}");
                return;
            }
            var answer = await pc.CreateAnswer().ConfigureAwait(false);
            await pc.SetLocalDescription(answer).ConfigureAwait(false);
            
            // --- send answer SDP -> Signaling Server
            var answerSDP = new Answer { client = offer.client, sdp = answer.sdp };
            var answerMsg = signaling.SendMessage(nameof(Answer), answerSDP);
            answerMsg.EventTargets = new EventTargets(); // send message only to SignalingService not to clients
            await signaling.SyncTasks().ConfigureAwait(false);

            _ = socketHost.SendReceiveMessages();
        }
        
        private void ProcessIceCandidate(Message<ClientIce> message, EventContext context) {
            if (!message.GetParam(out var value, out var error)) {
                logger.Log(HubLog.Error, $"invalid client ICE candidate. error: {error}");
                return;                    
            }
            if (!clients.TryGetValue(context.SrcClient, out var socketHost)) {
                logger.Log(HubLog.Error, $"client not found. client: {context.SrcClient}");
                return;
            }
            var parseCandidate= IceCandidate.TryParse(value.candidate.AsString(), out var iceCandidate);
            if (!parseCandidate) {
                logger.Log(HubLog.Error, "invalid ICE candidate");
            }
            socketHost.pc.AddIceCandidate(iceCandidate);
        }
    }
}
