// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using SIPSorcery.Net;
using TinyJson;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public class RtcHost : IHost
    {
        private readonly string                                 name;
        private readonly WebRtcConfig                           config;
        private readonly Signaling                              signaling;
        private readonly Dictionary<ShortString, RtcSocketHost> clients;
        private readonly IHubLogger                             logger;
        
        public RtcHost (SocketClientHub signalingHub, string name, WebRtcConfig config, string user, string token, SharedEnv env = null) {
            this.name   = name;
            this.config = config;
            clients     = new Dictionary<ShortString, RtcSocketHost>(ShortString.Equality);
            signaling   = new Signaling(signalingHub) { UserId = user, Token = token };
            logger      = signaling.Logger;
        }
        
        public async Task Register(HttpHost host)
        {
            signaling.SubscribeMessage<Offer>(nameof(Offer), async (message, context) =>
            {
                if (!message.GetParam(out var offer, out var error)) {
                    logger.Log(HubLog.Error, $"invalid Offer. error: {error}");
                    return;
                }
                var rtcConfig   = config.GetRtcConfiguration();
                var socketHost  = new RtcSocketHost(rtcConfig, offer.client.ToString(), host.hub, this);
                var pc          = socketHost.pc;
                var dc          = await pc.createDataChannel("test").ConfigureAwait(false); // right after connection creation. Otherwise: NoRemoteMedia 
                dc.onmessage += (channel, protocol, data) => {
                    logger.Log(HubLog.Info, "onmessage");
                };
                dc.onopen += ()         => { logger.Log(HubLog.Info, "datachannel onopen"); };
                dc.onclose += ()        => { logger.Log(HubLog.Info, "datachannel onclose"); };
                dc.onerror += dcError   => { logger.Log(HubLog.Error, $"datachannel onerror: {dcError}"); };
                
                clients.Add(offer.client, socketHost);
                
                pc.onicecandidate += candidate => {
                    // send ICE candidate to WebRTC client
                    var jsonCandidate   = new JsonValue(candidate.ToJson());
                    var msg             = signaling.SendMessage(nameof(HostIce), new HostIce { candidate = jsonCandidate });
                    msg.EventTargetClient(offer.client);
                    _ = signaling.SyncTasks();
                };
                var rtcOffer = new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = offer.sdp };
                var setRemoteResult = pc.setRemoteDescription(rtcOffer);
                if (setRemoteResult != SetDescriptionResultEnum.OK) {
                    logger.Log(HubLog.Error, $"setRemoteDescription failed. result: {setRemoteResult}");
                    return;
                }
                var answer = pc.createAnswer();
                await pc.setLocalDescription(answer).ConfigureAwait(false);
                
                // send answer SDP -> Signaling Server
                var answerSDP = new Answer { client = offer.client, sdp = answer.sdp };
                var answerMsg = signaling.SendMessage(nameof(Answer), answerSDP);
                answerMsg.EventTargets = new EventTargets(); // send message only to SignalingService not to clients
                await signaling.SyncTasks();

                _ = socketHost.SendReceiveMessages();
            });
            signaling.SubscribeMessage<ClientIce>(nameof(ClientIce), (message, context) => {
                if (!message.GetParam(out var value, out var error)) {
                    logger.Log(HubLog.Error, $"invalid client ICE candidate. error: {error}");
                    return;                    
                }
                if (!clients.TryGetValue(context.SrcClient, out var socketHost)) {
                    logger.Log(HubLog.Error, $"client not found. client: {context.SrcClient}");
                    return;
                }
                var parseCandidate= RTCIceCandidateInit.TryParse(value.candidate.AsString(), out var iceCandidateInit);
                if (!parseCandidate) {
                    logger.Log(HubLog.Error, "invalid ICE candidate");
                }
                socketHost.pc.addIceCandidate(iceCandidateInit);
            });
            signaling.RegisterHost(new RegisterHost { name = name });
            await signaling.SyncTasks().ConfigureAwait(false);
        }
    }
}