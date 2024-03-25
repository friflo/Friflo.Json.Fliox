// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
                if (wsConn != null && wsConn.socket.State == WebSocketState.Open)
                    return wsConn;
                return null;
            }
        }
        
        private Task<WebSocketConnection> JoinConnects(out TaskCompletionSource<WebSocketConnection> tcs, out WebSocketConnection connection) {
            lock (websocketLock) {
                if (connectTask != null) {
                    connection  = null;
                    tcs     = null;
                    return connectTask;
                }
                connection  = wsConnection = new WebSocketConnection();
                tcs         = new TaskCompletionSource<WebSocketConnection>();
                connectTask = tcs.Task;
                return connectTask;
            }
        }
        
        // static int count;
        
        private async Task<WebSocketConnection> Connect() {
            var task = JoinConnects(out var tcs, out WebSocketConnection connection);
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
            WebSocketConnection connection;
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
