// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public partial class WebRtcConfig
    {
        public  string      StunUrl { get; init; }

        public  override    string  ToString() => $"StunUrl: {StunUrl}";
    }
}
