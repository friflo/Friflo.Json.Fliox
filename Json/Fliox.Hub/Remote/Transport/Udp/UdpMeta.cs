// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    internal readonly struct UdpMeta
    {
        internal readonly   IPEndPoint  remoteEndPoint;

        public   override   string      ToString() => $"remote: {remoteEndPoint}";

        internal UdpMeta (IPEndPoint remoteEndPoint) {
            this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        }
    }
}