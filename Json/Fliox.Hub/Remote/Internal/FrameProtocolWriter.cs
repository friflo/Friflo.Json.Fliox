// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote.Internal
{
    public class FrameProtocolWriter
    {
        public async Task WriteAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
    }
}