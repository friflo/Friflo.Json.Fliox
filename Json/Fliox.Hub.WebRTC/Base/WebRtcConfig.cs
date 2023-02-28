// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed partial class WebRtcConfig
    {
        public  IReadOnlyCollection<string>     IceServerUrls { get; init; }
        
#if !UNITY_5_3_OR_NEWER
        private SIPSorcery.Net.RTCConfiguration rtcConfiguration;    
#endif

        public  override    string                      ToString() => $"IceServerUrls: {string.Join(", ", IceServerUrls)}";
    }
}
