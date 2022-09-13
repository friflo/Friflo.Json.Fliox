// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.WebSockets;
using System.Security.Principal;
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
            var websocket   = new ServerWebSocket();
            var wsContext   = new ServerWebSocketContext (websocket);
            var response    = context.Response;
            
            var headers     = new Dictionary<string, string> {
                { "Upgrade",        "websocket" },
                { "Connection",     "Upgrade"}
            };
            await HttpListenerExtensions.WriteResponseString(response, null, 101, "", headers).ConfigureAwait(false);
            return wsContext;
        }
    }
}