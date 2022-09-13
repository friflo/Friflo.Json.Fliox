// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable UnassignedGetOnlyAutoProperty
namespace Friflo.Json.Fliox.Hub.Remote.Internal
{
    internal class ServerWebSocketContext : WebSocketContext
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
        internal static async Task<ServerWebSocketContext> AcceptWebSocket(this HttpListenerContext context) {
            var websocket           = new ServerWebSocket();
            var wsContext           = new ServerWebSocketContext (websocket);
            var headers             = context.Request.Headers;
            var secWebSocketKey     = headers["Sec-WebSocket-Key"];
            var secWebSocketProtocol= headers["Sec-WebSocket-Protocol"];
            var secWebSocketVersion = headers["Sec-WebSocket-Version"];
            
            // --- create response
            // [WebSocket - Wikipedia] https://en.wikipedia.org/wiki/WebSocket
            // secWebSocketKey = "x3JJHMbDL1EzLkh9GBhXDw=="; // test from Wikipedia
            var secWebSocketAccept      = Sha1Hash(secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
            var response                = context.Response;
            response.StatusCode         = 101;
            response.StatusDescription  = "Switching Protocols";
            var responseHeaders         = response.Headers;
            responseHeaders["Upgrade"]                  = "websocket";
            responseHeaders["Connection"]               = "Upgrade";
            responseHeaders["Sec-WebSocket-Accept"]     = secWebSocketAccept;
            
            await response.OutputStream.FlushAsync();
            return wsContext;
        }
        
        private static string Sha1Hash(string input) {
            using (SHA1Managed sha1 = new SHA1Managed()) {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hash);
            }
        }
    }
}