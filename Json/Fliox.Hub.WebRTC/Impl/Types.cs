// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    
    internal enum IceConnectionState
    {
        closed,
        failed,
        disconnected,
        @new,
        checking,
        connected,
    }
    
    internal enum DataChannelState
    {
        connecting,
        open,
        closing,
        closed,
    }
    
    internal enum SignalingState
    {
        Stable,
        HaveLocalOffer,
        HaveLocalPrAnswer,
        HaveRemoteOffer,
        HaveRemotePrAnswer,
        Closed
    }
    
    internal enum SdpType
    {
        answer,
        offer,
    }

}