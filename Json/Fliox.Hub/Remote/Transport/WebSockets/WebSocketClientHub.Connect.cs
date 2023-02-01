// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public sealed partial class WebSocketClientHub
    {
        private WebSocketConnection GetWebsocketConnection() {
            lock (websocketLock) {
                var wsConn = wsConnection;
                if (wsConn != null && wsConn.websocket.State == WebSocketState.Open)
                    return wsConn;
                return null;
            }
        }
        
        private Task<WebSocketConnection> JoinConnects(out TaskCompletionSource<WebSocketConnection> tcs, out WebSocketConnection wsConn) {
            lock (websocketLock) {
                if (connectTask != null) {
                    wsConn  = null;
                    tcs     = null;
                    return connectTask;
                }
                wsConn  = wsConnection = new WebSocketConnection();
                tcs     = new TaskCompletionSource<WebSocketConnection>();
                connectTask = tcs.Task;
                return connectTask;
            }
        }
        
        // static int count;
        
        private async Task<WebSocketConnection> Connect() {
            var task = JoinConnects(out var tcs, out WebSocketConnection wsConn);
            if (tcs == null) {
                wsConn = await task.ConfigureAwait(false);
                return wsConn;
            }

            // Console.WriteLine($"WebSocketClientHub.Connect() endpoint: {endpoint}");
            // ws.Options.SetBuffer(4096, 4096);
            try {
                // Console.WriteLine($"Connect {++count}");
                await wsConn.websocket.ConnectAsync(endpointUri, CancellationToken.None).ConfigureAwait(false);

                connectTask = null;
                tcs.SetResult(wsConn);
            } catch (Exception e) {
                connectTask = null;
                tcs.SetException(e);
                throw;
            }
            try {
                _ = RunReceiveMessageLoop(wsConn).ConfigureAwait(false);
            } catch (Exception e) {
                Debug.Fail("ReceiveLoop() failed", e.Message);
            }
            return wsConn;
        }
        
        public async Task Close() {
            WebSocketConnection wsConn;
            lock (websocketLock) {
                wsConn = wsConnection;
                if (wsConn == null || wsConn.websocket.State == WebSocketState.Closed)
                    return;
                wsConnection = null;
            }
            await wsConn.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }
    }
}
