// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;
using SIPSorcery.Net;
using TinyJson;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public class RtcHost
    {
        private readonly Signaling                              signaling;
        private readonly Dictionary<ShortString, RtcSocketHost> clients;
        private readonly IHubLogger                             Logger;
        
        public RtcHost (Signaling signaling) {
            this.signaling  = signaling;
            Logger          = this.signaling.Logger;
            clients         = new Dictionary<ShortString, RtcSocketHost>(ShortString.Equality);
        }
        
        public async Task Register(string name, HttpHost host, WebRtcConfig config)
        {
            signaling.SubscribeMessage<Offer>(nameof(Offer), async (message, context) =>
            {
                if (!message.GetParam(out var offer, out var error)) {
                    Logger.Log(HubLog.Error, $"invalid Offer. error: {error}");
                    return;
                }
                var rtcConfig       = config.GetRtcConfiguration();
                var socketHost      = new RtcSocketHost(rtcConfig, offer.client.ToString(), host.hub, null);
                var rtcConnection   = socketHost.connection;
                var rtcOffer        = new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = offer.sdp };
                rtcConnection.setRemoteDescription(rtcOffer);
                var answer = rtcConnection.createAnswer();
                clients.Add(offer.client, socketHost);
                
                rtcConnection.onicecandidate += candidate => {
                    // send ICE candidate to WebRTC client
                    var jsonCandidate   = new JsonValue(candidate.candidate.ToJson());
                    var msg             = signaling.SendMessage(nameof(HostIce), new HostIce { candidate = jsonCandidate });
                    msg.EventTargetClient(offer.client);
                    _ = signaling.SyncTasks();
                };
                await rtcConnection.setLocalDescription(answer).ConfigureAwait(false);
                
                // send answer SDP -> Signaling Server
                var answerSDP = new Answer { client = offer.client, sdp = answer.sdp };
                var answerMsg = signaling.SendMessage(nameof(Answer), answerSDP);
                answerMsg.EventTargets = new EventTargets(); // send message only to SignalingService not to clients
                await signaling.SyncTasks();

                _ = socketHost.SendReceiveMessages();
            });
            signaling.SubscribeMessage<ClientIce>(nameof(ClientIce), (message, context) => {
                if (!message.GetParam(out var value, out var error)) {
                    Logger.Log(HubLog.Error, $"invalid client ICE candidate. error: {error}");
                    return;                    
                }
                if (!clients.TryGetValue(context.SrcClient, out var socketHost)) {
                    Logger.Log(HubLog.Error, $"client not found. client: {context.SrcClient}");
                    return;
                }
                if (!RTCIceCandidateInit.TryParse(value.candidate.AsString(), out var iceCandidateInit)) {
                    Logger.Log(HubLog.Error, "invalid ICE candidate");
                    return;
                }
                socketHost.connection.addIceCandidate(iceCandidateInit);
            });
            signaling.RegisterHost(new RegisterHost { name = name });
            await signaling.SyncTasks().ConfigureAwait(false);
        }
    }
}