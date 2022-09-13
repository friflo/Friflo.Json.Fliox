// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote.RFC6455;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Hub.Remote.Internal
{
    internal sealed class ServerWebSocket : WebSocket
    {
        private readonly    NetworkStream           stream;
        private readonly    FrameProtocol           protocol = new FrameProtocol();
        //
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
            await protocol.ReadFrame(stream, buffer, cancellationToken);

            return new WebSocketReceiveResult(protocol.ByteCount, protocol.MessageType, protocol.EndOfMessage);
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) {
            await stream.WriteAsync(buffer, cancellationToken);
        }
        // ---------------------------------------------------------------------------------------------

        internal ServerWebSocket(NetworkStream stream) {
            state       = WebSocketState.Open;
            this.stream = stream;
        }
    }
}