// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote.WebSockets;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public static class HttpListenerExtensions
    {
        public static async Task<RequestContext> ExecuteFlioxRequest (this HttpListenerContext context, HttpHost httpHost) {
            var request = context.Request;
            var url     = request.Url;
            var path    = url.LocalPath;
            if (!httpHost.GetRoute(path, out string route)) {
                return httpHost.GetRequestContext(path, request.HttpMethod);
            }
            HttpListenerRequest  req  = context.Request;
#if UNITY_5_3_OR_NEWER
            // Unity <= 2021.3 has currently no support for Server WebSockets  =>  add functionality using ServerWebSocketExtensions
            // see: [Help Wanted - Websocket Server in Standalone build - Unity Forum] https://forum.unity.com/threads/websocket-server-in-standalone-build.1072526/
            if (ServerWebSocketExtensions.IsWebSocketRequest(req)) {
                var wsContext       = await ServerWebSocketExtensions.AcceptWebSocket(context).ConfigureAwait(false);
                var websocket       = wsContext.WebSocket;
                var remoteEndPoint  = request.RemoteEndPoint;
                await WebSocketHost.SendReceiveMessages (websocket, remoteEndPoint, httpHost).ConfigureAwait(false);
                return null;
            }
#endif
            if (req.IsWebSocketRequest) {
                var wsContext       = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                var websocket       = wsContext.WebSocket;
                var remoteEndPoint  = request.RemoteEndPoint;
                await WebSocketHost.SendReceiveMessages (websocket, remoteEndPoint, httpHost).ConfigureAwait(false);
                return null;
            }
            var headers         = new HttpListenerHeaders(request.Headers);
            var cookies         = new HttpListenerCookies(request.Cookies);
            var requestContext  = new RequestContext(httpHost, request.HttpMethod, route, url.Query, req.InputStream, headers, cookies);
            await httpHost.ExecuteHttpRequest(requestContext).ConfigureAwait(false);
            return requestContext;
        }
        
        public static async Task WriteFlioxResponse(this HttpListenerContext context, RequestContext requestContext) {
            if (requestContext == null)
                return; // request was WebSocket
            HttpListenerResponse resp = context.Response;
            if (!requestContext.Handled)
                return;
            var responseBody = requestContext.Response;
            SetResponseHeader(resp, requestContext.ResponseContentType, requestContext.StatusCode, responseBody.Length, requestContext.ResponseHeaders);
            await resp.OutputStream.WriteAsync(responseBody, 0, responseBody.Length).ConfigureAwait(false);
            resp.Close();
        }
        
        public static async Task WriteResponseString (HttpListenerResponse response, string contentType, int statusCode, string value, Dictionary<string, string> headers) {
            byte[]  resultBytes = Encoding.UTF8.GetBytes(value);
            
            SetResponseHeader(response, contentType, statusCode, resultBytes.Length, headers);
            await response.OutputStream.WriteAsync(resultBytes, 0, resultBytes.Length).ConfigureAwait(false);
        }
        
        private static void SetResponseHeader (HttpListenerResponse response, string contentType, int statusCode, int len, Dictionary<string, string> headers) {
            response.ContentType        = contentType;
            response.ContentEncoding    = Encoding.UTF8;
            response.ContentLength64    = len;
            response.StatusCode         = statusCode;
        //  resp.Headers["link"]    = "rel=\"icon\" href=\"#\""; // not working as expected - expect no additional request of favicon.ico
            if (headers != null) {
                foreach (var header in headers) {
                    response.Headers[header.Key] = header.Value;
                }
            }
        }
    }
    
    internal class HttpListenerHeaders : IHttpHeaders {
        private  readonly   NameValueCollection headers;
        
        public              string              this[string key] => headers[key];
        
        internal HttpListenerHeaders(NameValueCollection headers) {
            this.headers = headers;    
        }
    }
    
    internal class HttpListenerCookies : IHttpCookies {
        private  readonly   CookieCollection    cookies;
        
        public              string              this[string key] => cookies[key]?.Value;
        
        internal HttpListenerCookies(CookieCollection cookies) {
            this.cookies = cookies;    
        }
    }
}
