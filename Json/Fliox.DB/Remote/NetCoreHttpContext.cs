// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.DB.Remote
{
    // WIP - Not used. Not sure if it is a good idea.
    public static class NetCoreHttpContext
    {
        static Type         HttpContext;
        static PropertyInfo WebSockets;
        static PropertyInfo IsWebSocketRequest;
        static MethodInfo   AcceptWebSocketAsync;
        //
        static PropertyInfo Request;
        static PropertyInfo RequestMethod;
        static PropertyInfo RequestPath;

        static NetCoreHttpContext () {
            HttpContext             = Type.GetType("Microsoft.AspNetCore.Http.HttpContext");
            WebSockets              = HttpContext.GetProperty("WebSockets");
            IsWebSocketRequest      = WebSockets.GetType().GetProperty("IsWebSocketRequest");
            AcceptWebSocketAsync    = WebSockets.GetType().GetMethod("AcceptWebSocketAsync");
            //
            Request                 = HttpContext.GetProperty("Request");
            RequestMethod           = HttpContext.GetType().GetProperty("Method");
            RequestPath             = HttpContext.GetType().GetProperty("Path");
            
            
        }
        
        public static async Task HandleRequest(object context, RemoteHostDatabase hostDatabase) {
            var webSockets          = WebSockets.GetValue(context);
            var isWebSocketRequest  = (bool)IsWebSocketRequest.GetValue(webSockets);
            if (isWebSocketRequest) {
                var ws = (WebSocket) AcceptWebSocketAsync.Invoke(webSockets, null);
                await WebSocketHost.SendReceiveMessages(ws, hostDatabase);
                return;
            }
            var request = Request.GetValue(context);
            // ...
        }
    }
}