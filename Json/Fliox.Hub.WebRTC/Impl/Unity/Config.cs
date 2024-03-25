// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System.Linq;
using Unity.WebRTC;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Impl
{
    internal sealed class RtcConfig
    {
        internal        RTCConfiguration impl;
        
        private RtcConfig(RTCConfiguration impl) {
            this.impl = impl;
        }
        
        internal static RtcConfig GetRtcConfiguration(WebRtcConfig config)
        {
            var server = new RTCIceServer { urls = config.IceServerUrls.ToArray() }; 
            var impl = new RTCConfiguration {
                iceServers = new []  {server  }
            };
            return new RtcConfig(impl);
        }
    }
}

#endif