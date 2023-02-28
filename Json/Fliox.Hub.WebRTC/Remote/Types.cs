// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    internal enum PeerConnectionState
    {
        closed,
        failed,
        disconnected,
        @new,
        connecting,
        connected,
    }
    
    internal enum DataChannelState
    {
        connecting,
        open,
        closing,
        closed,
    }
    
    internal sealed class IceCandidateModel {
        public  string  candidate;
        public  string  sdpMid;
        public  int     sdpMLineIndex;
        public  string  usernameFragment;
    }
}