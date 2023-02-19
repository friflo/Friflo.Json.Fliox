// Copyright (c) Ullrich Praetz. All rights reserved.
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

        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        private static extern int Close(IntPtr handle);

        // Workaround for blocking Socket.Close() on macos
        // see [UdpClient hangs when Receive is waiting and gets a Close() in macos · Issue #64551 · dotnet/runtime]
        //     https://github.com/dotnet/runtime/issues/64551
        internal static void CloseSocket(Socket socket) {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    // throw SocketException with SocketErrorCode == SocketError.OperationAborted in blocking Socket.Receive()
                    Close(socket.Handle);
                    return;                
            }
            socket.Close();
        }
    }
}