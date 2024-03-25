// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable UnassignedGetOnlyAutoProperty
namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    internal sealed class ServerWebSocketContext : WebSocketContext
    {
        public  override    CookieCollection    CookieCollection        { get; }
        public  override    NameValueCollection Headers                 { get; }
        public  override    bool                IsAuthenticated         { get; }
        public  override    bool                IsLocal                 { get; }
        public  override    bool                IsSecureConnection      { get; }
        public  override    string              Origin                  { get; }
        public  override    Uri                 RequestUri              { get; }
        public  override    string              SecWebSocketKey         { get; }
        public  override    IEnumerable<string> SecWebSocketProtocols   { get; }
        public  override    string              SecWebSocketVersion     { get; }
        public  override    IPrincipal          User                    { get; }
        public  override    WebSocket           WebSocket               { get; }
        
        internal ServerWebSocketContext (WebSocket webSocket) {
            WebSocket = webSocket;
        }
    }
    
    internal static class ServerWebSocketExtensions
    {
        internal static async Task<ServerWebSocketContext> AcceptWebSocket(HttpListenerContext context) {
            var (stream, socket)    = GetNetworkStream(context);
            var websocket           = new ServerWebSocket(stream, socket);
            var wsContext           = new ServerWebSocketContext (websocket);
            var headers             = context.Request.Headers;
            var secWebSocketKey     = headers["Sec-WebSocket-Key"];
            var secWebSocketProtocol= headers["Sec-WebSocket-Protocol"];
            var secWebSocketVersion = headers["Sec-WebSocket-Version"];
            
            // var items = headers.AllKeys.SelectMany(headers.GetValues, (k, v) => new {key = k, value = v});
            
            // --- create response
            var secWebSocketAccept      = Sha1Hash(secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
            // [WebSocket - Wikipedia] https://en.wikipedia.org/wiki/WebSocket
            // secWebSocketKey = "x3JJHMbDL1EzLkh9GBhXDw=="; // test from Wikipedia
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 101 Switching Protocols\r\n");
            sb.Append("Connection: Upgrade\r\n");
            sb.Append("Upgrade: websocket\r\n");
            sb.Append($"Sec-WebSocket-Accept: {secWebSocketAccept}\r\n");
            sb.Append("\r\n");
            var     response        = sb.ToString();
            byte[]  responseBytes   = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
            
            await stream.FlushAsync().ConfigureAwait(false); // todo required?

            return wsContext;
        }
        
        private static readonly PropertyInfo ConnectionInfo;
        private static readonly FieldInfo    StreamInfo;
        private static readonly PropertyInfo SocketInfo;

        static ServerWebSocketExtensions() {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            ConnectionInfo      = typeof(HttpListenerContext).GetProperty("Connection", flags);
            if (ConnectionInfo == null) throw new NullReferenceException (nameof(ConnectionInfo));
            var connectionType  = ConnectionInfo.PropertyType;  // HttpConnection
            StreamInfo          = connectionType.GetField("stream", flags);
            if (StreamInfo == null) throw new NullReferenceException (nameof(StreamInfo));
            SocketInfo          = typeof(NetworkStream).GetProperty("Socket", flags);
            if (SocketInfo == null) throw new NullReferenceException (nameof(SocketInfo));
        }
        
        private static (NetworkStream, Socket) GetNetworkStream(HttpListenerContext context) {
            var connection  = ConnectionInfo.GetValue(context);
            var stream      = (NetworkStream) StreamInfo.GetValue(connection);
            var socket      = (Socket)SocketInfo.GetValue(stream);
            return (stream, socket);
        }
        
        private static string Sha1Hash(string input) {
            using (SHA1Managed sha1 = new SHA1Managed()) {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hash);
            }
        }

        internal static bool IsWebSocketRequest(HttpListenerRequest req) {
            if (req.Headers["Upgrade"] != "websocket")
                return false;
            var connection = req.Headers["Connection"];
            // Chrome:  Connection: Upgrade
            // Firefox: Connection: keep-alive, Upgrade
            if (!connection.Contains("Upgrade"))
                return false;
            return true;
        }
    }
}