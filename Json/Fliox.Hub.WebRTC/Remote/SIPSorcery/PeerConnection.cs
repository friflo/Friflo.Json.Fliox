// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using SIPSorcery.Net;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    internal sealed class PeerConnection
    {
        private     readonly    RTCPeerConnection           impl;
        internal    event       Action<PeerConnectionState> OnConnectionStateChange;
        internal    event       Action<DataChannel>         OnDataChannel;
        internal    event       Action<IceCandidate>        OnIceCandidate;

        internal PeerConnection (WebRtcConfig config) {
            impl = new RTCPeerConnection(config.Get().impl);
            impl.onconnectionstatechange += state => {
                PeerConnectionState connState;
                switch (state) {
                    case RTCPeerConnectionState.closed:         connState = PeerConnectionState.closed;         break;
                    case RTCPeerConnectionState.failed:         connState = PeerConnectionState.failed;         break;
                    case RTCPeerConnectionState.disconnected:   connState = PeerConnectionState.disconnected;   break;
                    case RTCPeerConnectionState.@new:           connState = PeerConnectionState.@new;           break;
                    case RTCPeerConnectionState.connecting:     connState = PeerConnectionState.connecting;     break;
                    case RTCPeerConnectionState.connected:      connState = PeerConnectionState.connected;      break;
                    default:
                        throw new InvalidOperationException($"unexpected connection state: {state}");
                    
                }
                OnConnectionStateChange?.Invoke(connState);
            };
            impl.ondatachannel += channel => {
                var dc = new DataChannel(channel);
                OnDataChannel?.Invoke(dc);
            };
            impl.onicecandidate += candidate => {
                OnIceCandidate?.Invoke(new IceCandidate(candidate));
            };
        }
        
        internal async Task<DataChannel> CreateDataChannel(string label) {
            var dc = await impl.createDataChannel(label);
            return new DataChannel(dc);
        }
        
        internal SessionDescription CreateOffer() {
            return new SessionDescription(impl.createOffer());  
        }
        
        internal SessionDescription CreateAnswer() {
            return new SessionDescription(impl.createAnswer());  
        }
        
        internal  bool SetRemoteDescription(SessionDescription desc, out string error) {
            var result = impl.setRemoteDescription(desc.impl);
            if (result == SetDescriptionResultEnum.OK) {
                error = null;
                return true;
            }
            error = result.ToString();
            return false;
        }
        
        internal async Task SetLocalDescription(SessionDescription desc) {
            await impl.setLocalDescription(desc.impl);
        }
        
        internal void AddIceCandidate(IceCandidate candidate) {
            var i       = candidate.impl;
            var iceInit = new RTCIceCandidateInit {
                candidate           = i.candidate,
                sdpMid              = i.sdpMid,
                usernameFragment    = i.usernameFragment,
                sdpMLineIndex       = i.sdpMLineIndex
            };
            impl.addIceCandidate(iceInit);
        }
    }
    
    internal enum SdpType
    {
        answer,
        offer,
    }
    
    internal sealed class SessionDescription
    {
        internal readonly   RTCSessionDescriptionInit   impl;
        
        internal    string  sdp { get => impl.sdp; init => impl.sdp = value; }
        internal    SdpType  type {
            get {
                switch (impl.type) {
                    case RTCSdpType.answer: return SdpType.answer;
                    case RTCSdpType.offer:  return SdpType.offer;
                    default: throw new InvalidOperationException($"unexpected type: {impl.type}");
                }
            }
            init {
                switch (value) {
                    case SdpType.answer:    impl.type = RTCSdpType.answer;  return;
                    case SdpType.offer:     impl.type = RTCSdpType.offer;   return;
                    default: throw new InvalidOperationException($"unexpected type: {value}");
                }
            }
        }
        
        internal SessionDescription() {
            impl = new RTCSessionDescriptionInit();
        }

        internal SessionDescription(RTCSessionDescriptionInit impl) {
            this.impl = impl;
        }
    }
    
    internal sealed class IceCandidate {
        
        internal readonly RTCIceCandidate impl;
        
        internal IceCandidate(RTCIceCandidate impl) {
            this.impl = impl;
        }
        
        internal string ToJson() {
            var model = new IceCandidateModel {
                candidate           = impl.candidate,
                sdpMid              = impl.sdpMid,
                sdpMLineIndex       = impl.sdpMLineIndex,
                usernameFragment    = impl.usernameFragment
            };
            return JsonSerializer.Serialize(model);
        }

        public static bool TryParse(string value, out IceCandidate candidate) {
            var model   = JsonSerializer.Deserialize<IceCandidateModel>(value);
            var init    = new RTCIceCandidateInit {
                candidate           = model.candidate,
                sdpMid              = model.sdpMid,
                sdpMLineIndex       = (ushort)model.sdpMLineIndex,
                usernameFragment    = model.usernameFragment,
            };
            var iceCandidate    = new RTCIceCandidate (init);
            candidate           = new IceCandidate(iceCandidate);
            return true;
        }
    }
}

#endif
