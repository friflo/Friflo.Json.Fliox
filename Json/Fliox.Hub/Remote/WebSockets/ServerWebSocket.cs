// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    /// <summary>
    /// A server <see cref="WebSocket"/> implementation.<br/>
    /// This implementation is required for Unity as it does not provide an implementation of <c>System.Net.WebSockets.ServerWebSocket</c>.
    /// </summary>
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

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) {
            var frame = buffer.Array;
            var socketState = await reader.ReadFrame(stream, frame, cancellationToken).ConfigureAwait(false);
            if (socketState == WebSocketState.Open) {
                return new WebSocketReceiveResult(reader.ByteCount, reader.MessageType, reader.EndOfMessage);
            }
            state                   = reader.SocketState;
            closeStatus             = reader.CloseStatus;
            closeStatusDescription  = reader.CloseStatusDescription;
            
            return new WebSocketReceiveResult(reader.ByteCount, reader.MessageType, reader.EndOfMessage, closeStatus, closeStatusDescription);
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) {
            await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            var data        = buffer.Array;
            var dataOffset  = buffer.Offset;
            var dataLen     = buffer.Count;
            try {
                await writer.WriteFrame(stream, data, dataOffset, dataLen, messageType, endOfMessage, cancellationToken).ConfigureAwait(false);
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
            // don't block when closing socket by pending data in outgoing network buffer
            socket.LingerState = new LingerOption(false, 0);
            // disable Nagle algorithm
            socket.NoDelay  = true;
        }
        
        public override void Dispose() {
            sendLock.Dispose();
            socket.Dispose();
            stream.Dispose();
        }
        
        private void Close() {
            // - stream does not close underlying socket => close it explicit
            // - Close(0) close socket instantaneously without blocking
            socket.Close(0);
            stream.Close();
        }
    }
}