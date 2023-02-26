// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using SIPSorcery.Net;
using TinyJson;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public class RtcHost
    {
        private readonly Signaling signaling;
        
        public RtcHost (Signaling signaling) {
            this.signaling = signaling;
        }
        
        public async Task Register(string name, HttpHost host, WebRtcConfig config)
        {
            signaling.SubscribeMessage<Offer>(nameof(Offer), async (message, context) =>
            {
                var rtcConfig   = config.GetRtcConfiguration();
                message.GetParam(out var offer, out _);
                var socketHost      = new RtcSocketHost(rtcConfig, "remoteClient", host.hub, null);
                var rtcConnection   = socketHost.connection;
                var rtcOffer        = new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = offer.sdp };
                rtcConnection.setRemoteDescription(rtcOffer);
                var answer = rtcConnection.createAnswer();
                
                rtcConnection.onicecandidate += candidate => {
                    var value   = new JsonValue(candidate.candidate.ToJson());
                    var msg     = signaling.SendMessage(nameof(HostIceCandidate), new HostIceCandidate { value = value });
                    msg.EventTargets.AddClient(context.SrcClient);
                    _ = signaling.SyncTasks();
                };
                await rtcConnection.setLocalDescription(answer).ConfigureAwait(false);
                
                // send answer SDP -> Signaling Server -> WebRTC Client
                var answerSDP = new Answer { client = offer.client, sdp = answer.sdp };
                var answerMsg = signaling.SendMessage(nameof(Answer), answerSDP);
                answerMsg.EventTargets.AddClients(new List<string>()); // send message only to SignalingService not to clients
                await signaling.SyncTasks();

                _ = socketHost.SendReceiveMessages();
            });
            signaling.SubscribeMessage<ClientIceCandidate>(nameof(ClientIceCandidate), (message, context) => {
                
            });
            signaling.RegisterHost(new RegisterHost { name = name });
            await signaling.SyncTasks().ConfigureAwait(false);
        }
    }
}