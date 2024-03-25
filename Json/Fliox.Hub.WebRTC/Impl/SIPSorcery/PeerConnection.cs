// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using SIPSorcery.Net;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Impl
{
    internal sealed class PeerConnection
    {
        private     readonly    RTCPeerConnection           impl;
        internal                SignalingState              SignalingState => GetSignalingState();
        
        private     Action<PeerConnectionState> onConnectionStateChange;
        internal    Action<PeerConnectionState> OnConnectionStateChange     { get => onConnectionStateChange; set => SetOnConnectionStateChange(value); }
        
        private     Action<DataChannel>         onDataChannel;
        internal    Action<DataChannel>         OnDataChannel               { get => onDataChannel; set => SetOnDataChannel(value); }
        
        private     Action<IceCandidate>        onIceCandidate;
        internal    Action<IceCandidate>        OnIceCandidate              { get => onIceCandidate; set => SetOnIceCandidate(value); }
        
        private     Action<IceConnectionState>  onIceConnectionStateChange;
        internal    Action<IceConnectionState>  OnIceConnectionStateChange  { get => onIceConnectionStateChange; set => SetOnIceConnectionStateChange(value); }
        
        private void SetOnConnectionStateChange(Action<PeerConnectionState> action) {
            onConnectionStateChange         = action;
            impl.onconnectionstatechange   += state => {
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
                action.Invoke(connState);
            };
        }
        
        private void SetOnDataChannel(Action<DataChannel> action) {
            onDataChannel       = action;
            impl.ondatachannel += channel => {
                var dc = new DataChannel(channel); action.Invoke(dc);
            };
        }
        
        private void SetOnIceCandidate(Action<IceCandidate> action) {
            onIceCandidate       = action;
            impl.onicecandidate += candidate => {
                var dc = new IceCandidate(candidate); action.Invoke(dc);
            };
        }
        
        private void SetOnIceConnectionStateChange(Action<IceConnectionState> action) {
            onIceConnectionStateChange       = action;
            impl.oniceconnectionstatechange += state => {
                IceConnectionState connState;
                switch (state) {
                    case RTCIceConnectionState.closed:          connState = IceConnectionState.closed;         break;
                    case RTCIceConnectionState.failed:          connState = IceConnectionState.failed;         break;
                    case RTCIceConnectionState.disconnected:    connState = IceConnectionState.disconnected;   break;
                    case RTCIceConnectionState.@new:            connState = IceConnectionState.@new;           break;
                    case RTCIceConnectionState.checking:        connState = IceConnectionState.checking;       break;
                    case RTCIceConnectionState.connected:       connState = IceConnectionState.connected;      break;
                    default:
                        throw new InvalidOperationException($"unexpected ice connection state: {state}");
                }
                action(connState);
            };
        }

        internal PeerConnection (WebRtcConfig config) {
            impl = new RTCPeerConnection(config.Get().impl);
        }
        
        internal async Task<DataChannel> CreateDataChannel(string label) {
            var init = new RTCDataChannelInit { ordered = true };
            var dc = await impl.createDataChannel(label, init).ConfigureAwait(false);
            return new DataChannel(dc);
        }
        
        internal Task<SessionDescription> CreateOffer() {
            var descImpl = impl.createOffer();
            return Task.FromResult(new SessionDescription(descImpl));  
        }
        
        internal Task<SessionDescription> CreateAnswer() {
            var descImpl = impl.createAnswer();
            return Task.FromResult(new SessionDescription(descImpl));  
        }
        
        internal Task<string> SetRemoteDescription(SessionDescription desc) {
            var result      = impl.setRemoteDescription(desc.impl);
            string error    = result == SetDescriptionResultEnum.OK ? null : result.ToString();
            return Task.FromResult(error);
        }
        
        internal async Task SetLocalDescription(SessionDescription desc) {
            await impl.setLocalDescription(desc.impl).ConfigureAwait(false);
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
        
        private SignalingState GetSignalingState() {
            switch (impl.signalingState) {
                case RTCSignalingState.stable:                  return SignalingState.Stable;
                case RTCSignalingState.have_local_offer:        return SignalingState.HaveLocalOffer;
                case RTCSignalingState.have_local_pranswer:     return SignalingState.HaveLocalPrAnswer;
                case RTCSignalingState.have_remote_offer:       return SignalingState.HaveRemoteOffer;
                case RTCSignalingState.have_remote_pranswer:    return SignalingState.HaveRemotePrAnswer;
                case RTCSignalingState.closed:                  return SignalingState.Closed;
                default: throw new InvalidOperationException($"unexpected signaling state: {impl.signalingState}");
            }
        }
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
    
    internal sealed class IceCandidate
    {
        
        internal readonly RTCIceCandidate impl;
        
        internal IceCandidate(RTCIceCandidate impl) {
            this.impl = impl;
        }
        
        internal string ToJson() {
            return impl.toJSON();
            /* var model = new IceCandidateModel {
                candidate           = impl.candidate,
                sdpMid              = impl.sdpMid,
                sdpMLineIndex       = impl.sdpMLineIndex,
                usernameFragment    = impl.usernameFragment
            };
            return JsonSerializer.Serialize(model); */
        }

        internal static bool TryParse(string value, out IceCandidate candidate) {
            RTCIceCandidateInit.TryParse(value, out var init);
            var iceCandidate    = new RTCIceCandidate (init);
            candidate           = new IceCandidate(iceCandidate);
            return true;
            /* var model   = JsonSerializer.Deserialize<IceCandidateModel>(value);
            var init    = new RTCIceCandidateInit {
                candidate           = model.candidate,
                sdpMid              = model.sdpMid,
                sdpMLineIndex       = (ushort)model.sdpMLineIndex,
                usernameFragment    = model.usernameFragment,
            };
            var iceCandidate    = new RTCIceCandidate (init);
            candidate           = new IceCandidate(iceCandidate);
            return true;*/
        }
    }
}

#endif
