// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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
    
    internal static class UdpUtils
    {
        internal static bool IsIgnorable(SocketException socketException) {
            switch (socketException.SocketErrorCode) {
                case SocketError.ConnectionReset: // (10054): An existing connection was forcibly closed by the remote host.
                    return true;
            }
            return false;
        }
        
        internal static bool IsClosed(SocketException socketException) {
            switch (socketException.SocketErrorCode) {
                case SocketError.Interrupted:
                case SocketError.TimedOut:      // (10060): socket is closed in Mono/Unity macos
                    return true;
            }
            return false;
        }

        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        private static extern int Close(IntPtr handle);

        /// <summary>
        /// Close passed <paramref name="socket"/>.<br/>
        /// </summary>
        /// <remarks>
        /// Close also unbind a socket from the local port previously associated to with <see cref="Socket.Bind"/>.<br/> 
        /// Includes a workaround to close a <see cref="Socket"/> using synchronous Receive() or Send() methods on macos.<br/>
        /// See: [UdpClient hangs when Receive is waiting and gets a Close() in macos · Issue #64551 · dotnet/runtime]
        /// https://github.com/dotnet/runtime/issues/64551
        /// </remarks>
        internal static void CloseSocket(Socket socket) {
#if !UNITY_5_3_OR_NEWER
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                // throw SocketException with SocketErrorCode == SocketError.OperationAborted in blocking Socket.Receive()
                Close(socket.Handle);
                return;                
            }
#endif
            socket.Close();
        }
    }
}