// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.WebRTC.Impl;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class SignalingConfig {
        public  string          SignalingDB     { get; init; } = "signaling";
        public  string          SignalingHost   { get; init; }
        public  string          User            { get; init; }
        public  string          Token           { get; init; }
        public  WebRtcConfig    WebRtcConfig    { get; init; }
    }
    
    public sealed class WebRtcConfig
    {
        public      IReadOnlyCollection<string> IceServerUrls { get; init; }
        private     RtcConfig                   cache;
        
        internal    RtcConfig                   Get() {
            if (cache != null) {
                return cache;
            }
            return cache = RtcConfig.GetRtcConfiguration(this);
        }

        public  override    string              ToString() => $"IceServerUrls: {string.Join(", ", IceServerUrls)}";
    }
}