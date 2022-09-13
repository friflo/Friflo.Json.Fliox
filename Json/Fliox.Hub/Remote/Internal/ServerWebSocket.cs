// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Hub.Remote.Internal
{
    internal sealed class ServerWebSocket : WebSocket
    {
        private             WebSocketCloseStatus?   closeStatus = null;
        private             string                  closeStatusDescription = null;
        private             WebSocketState          state;
        private             string                  subProtocol = null;

        public  override    WebSocketCloseStatus?   CloseStatus             => closeStatus;
        public  override    string                  CloseStatusDescription  => closeStatusDescription;
        public  override    WebSocketState          State                   => state;
        public  override    string                  SubProtocol             => subProtocol;
        
        public override void Abort() {
            throw new NotImplementedException();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) {
            await Task.Delay(100000);
            throw new NotImplementedException();
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
        // ---------------------------------------------------------------------------------------------

        internal ServerWebSocket() {
            state = WebSocketState.Open;
        }
    }
}