// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
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
