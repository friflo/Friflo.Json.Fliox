// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Unity.WebRTC;


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
            impl = new RTCPeerConnection(config.GetRtcConfiguration());
            impl.OnConnectionStateChange += state => {
                PeerConnectionState connState;
                switch (state) {
                    case RTCPeerConnectionState.Closed:         connState = PeerConnectionState.closed;         break;
                    case RTCPeerConnectionState.Failed:         connState = PeerConnectionState.failed;         break;
                    case RTCPeerConnectionState.Disconnected:   connState = PeerConnectionState.disconnected;   break;
                    case RTCPeerConnectionState.New:            connState = PeerConnectionState.@new;           break;
                    case RTCPeerConnectionState.Connecting:     connState = PeerConnectionState.connecting;     break;
                    case RTCPeerConnectionState.Connected:      connState = PeerConnectionState.connected;      break;
                    default:
                        throw new InvalidOperationException($"unexpected connection state: {state}");
                    
                }
                OnConnectionStateChange?.Invoke(connState);
            };
            impl.OnDataChannel += channel => {
                var dc = new DataChannel(channel);
                OnDataChannel?.Invoke(dc);
            };
            impl.OnIceCandidate += candidate => {
                OnIceCandidate?.Invoke(new IceCandidate(candidate));
            };
        }
        
        internal async Task<DataChannel> CreateDataChannel(string label) {
            var dc = await impl.CreateDataChannel(label);
            return new DataChannel(dc);
        }
        
        internal SessionDescription CreateOffer() {
            var asyncOp = impl.CreateOffer();
            return new SessionDescription(asyncOp.Desc);  
        }
        
        internal SessionDescription CreateAnswer() {
            var asyncOp = impl.CreateAnswer();
            return new SessionDescription(asyncOp.Desc);  
        }
        
        internal  bool SetRemoteDescription(SessionDescription desc, out string error) {
            var result = impl.SetRemoteDescription(ref desc.impl);
            if (!result.IsError) {
                error = null;
                return true;
            }
            error = result.ToString();
            return false;
        }
        
        internal async Task SetLocalDescription(SessionDescription desc) {
            await impl.SetLocalDescription(ref desc.impl);
        }
        
        internal void AddIceCandidate(IceCandidate candidate) {
            impl.AddIceCandidate(candidate.impl);
        }
    }
    
    internal enum SdpType
    {
        answer,
        offer,
    }
    
    internal sealed class SessionDescription
    {
        internal    RTCSessionDescription   impl;
        internal    string                  sdp { get => impl.sdp; init => impl.sdp = value; }
        internal    SdpType                 type {
            get {
                switch (impl.type) {
                    case RTCSdpType.Answer: return SdpType.answer;
                    case RTCSdpType.Offer:  return SdpType.offer;
                    default: throw new InvalidOperationException($"unexpected type: {impl.type}");
                }
            }
            init {
                switch (value) {
                    case SdpType.answer:    impl.type = RTCSdpType.Answer;  return;
                    case SdpType.offer:     impl.type = RTCSdpType.Offer;   return;
                    default: throw new InvalidOperationException($"unexpected type: {value}");
                }
            }
        }
        
        internal SessionDescription() {
            impl = new RTCSessionDescription();
        }

        internal SessionDescription(in RTCSessionDescription impl) {
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
        }

        public static bool TryParse(string value, out IceCandidate candidate) {
            bool success        = RTCIceCandidateInit.TryParse(value, out var candidateInit);
            var rtcIceCandidate = new RTCIceCandidate (candidateInit);
            candidate           = new IceCandidate(rtcIceCandidate);
            return success;
        }
    }
}

#endif
