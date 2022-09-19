// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    internal sealed class ServerWebSocket : WebSocket
    {
        private readonly    NetworkStream           stream;
        private readonly    Socket                  socket;
        private readonly    FrameProtocolReader     reader      = new FrameProtocolReader();
        private readonly    FrameProtocolWriter     writer      = new FrameProtocolWriter(false); // server must not mask payloads
        private readonly    SemaphoreSlim           sendLock    = new SemaphoreSlim(1);
        //
        private             WebSocketCloseStatus?   closeStatus;
        private             string                  closeStatusDescription;
        private             WebSocketState          state;
        private             string                  subProtocol = null;

        public  override    WebSocketCloseStatus?   CloseStatus             => closeStatus;
        public  override    string                  CloseStatusDescription  => closeStatusDescription;
        public  override    WebSocketState          State                   => state;
        public  override    string                  SubProtocol             => subProtocol;
        
        public override void Abort() {
            throw new NotImplementedException();
        }

        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                // Socket may already be closed by writer exception
                if (socket.Connected) {
                    await writer.CloseAsync (stream, closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                sendLock.Release();
                Close();
            }
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override void Dispose() {
            sendLock.Dispose();
            stream.Dispose();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> dataBuffer, CancellationToken cancellationToken) {
            var buffer = dataBuffer.Array;
            var socketState = await reader.ReadFrame(stream, buffer, cancellationToken).ConfigureAwait(false);
            if (socketState == WebSocketState.Open) {
                return new WebSocketReceiveResult(reader.ByteCount, reader.MessageType, reader.EndOfMessage);
            }
            state                   = reader.SocketState;
            closeStatus             = reader.CloseStatus;
            closeStatusDescription  = reader.CloseStatusDescription;
            
            return new WebSocketReceiveResult(reader.ByteCount, reader.MessageType, reader.EndOfMessage, closeStatus, closeStatusDescription);
        }

        public override async Task SendAsync(ArraySegment<byte> dataBuffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) {
            await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            var buffer = dataBuffer.Array;
            try {
                await writer.WriteFrame(stream, buffer, messageType, endOfMessage, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false); // todo required?
            }
            finally{
                sendLock.Release();
            }
        }
        // ---------------------------------------------------------------------------------------------

        internal ServerWebSocket(NetworkStream stream, Socket socket) {
            state       = WebSocketState.Open;
            this.stream = stream;
            this.socket = socket;
            // dont block when closing socket by pending data in outgoing network buffer
            socket.LingerState = new LingerOption(false, 0);
            // disable Nagle algorithm
            socket.NoDelay  = true;
        }
        
        private void Close() {
            // - stream does not close underlying socket => close it explicit
            // - Close(0) close socket instantaneously without blocking
            socket.Close(0);
            stream.Close();
        }
    }
}