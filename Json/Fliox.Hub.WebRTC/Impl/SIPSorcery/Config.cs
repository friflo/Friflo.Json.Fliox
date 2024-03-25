// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Linq;
using SIPSorcery.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Impl
{
    internal sealed class RtcConfig
    {
        internal readonly RTCConfiguration impl;
        
        private RtcConfig(RTCConfiguration impl) {
            this.impl = impl;
        }
        
        internal static RtcConfig GetRtcConfiguration(WebRtcConfig config)
        {
            var iceServers = config.IceServerUrls.Select(server => new RTCIceServer { urls = server }  ); 
            var impl = new RTCConfiguration {
                iceServers = iceServers.ToList()
            };
            return new RtcConfig(impl);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // This is needed to enable following features in .NET framework and .NET core <= 3.1 projects:
    // - init only setter properties. See [Init only setters - C# 9.0 draft specifications | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    // - record types
    internal static class IsExternalInit { }
}

#endif