// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Remote
{
    public sealed partial class WebRtcClientHub
    {
        private WebRtcConnection GetWebsocketConnection() {
            lock (websocketLock) {
                var wsConn = wsConnection;
                if (wsConn != null && wsConn.socket.State == WebSocketState.Open)
                    return wsConn;
                return null;
            }
        }
        
        private Task<WebRtcConnection> JoinConnects(out TaskCompletionSource<WebRtcConnection> tcs, out WebRtcConnection connection) {
            lock (websocketLock) {
                if (connectTask != null) {
                    connection  = null;
                    tcs     = null;
                    return connectTask;
                }
                connection  = wsConnection = new WebRtcConnection();
                tcs         = new TaskCompletionSource<WebRtcConnection>();
                connectTask = tcs.Task;
                return connectTask;
            }
        }
        
        // static int count;
        
        private async Task<WebRtcConnection> Connect() {
            var task = JoinConnects(out var tcs, out WebRtcConnection connection);
            if (tcs == null) {
                connection = await task.ConfigureAwait(false);
                return connection;
            }

            // Console.WriteLine($"WebSocketClientHub.Connect() endpoint: {endpoint}");
            // ws.Options.SetBuffer(4096, 4096);
            try {
                // Console.WriteLine($"Connect {++count}");
                await connection.socket.ConnectAsync(remoteHost, CancellationToken.None).ConfigureAwait(false);

                connectTask = null;
                tcs.SetResult(connection);
            } catch (Exception e) {
                connectTask = null;
                tcs.SetException(e);
                throw;
            }
            try {
                _ = RunReceiveMessageLoop(connection).ConfigureAwait(false);
            } catch (Exception e) {
                Debug.Fail("ReceiveLoop() failed", e.Message);
            }
            return connection;
        }
        
        public override async Task Close() {
            WebRtcConnection connection;
            lock (websocketLock) {
                connection = wsConnection;
                if (connection == null || connection.socket.State == WebSocketState.Closed)
                    return;
                wsConnection = null;
            }
            await connection.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }
    }
}

#endif