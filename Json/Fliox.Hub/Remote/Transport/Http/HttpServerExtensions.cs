// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote.WebSockets;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public enum AcceptWebSocketType
    {
        SystemNet,
        FrifloWebSockets
    }
    
    public static class HttpServerExtensions
    {
        /// <summary>
        /// Execute the request return a RequestContext containing the execution result.
        /// To return a HTTP response <see cref="WriteFlioxResponse"/> need to be called. 
        /// </summary>
        public static async Task<RequestContext> ExecuteFlioxRequest (this HttpListenerContext context, HttpHost httpHost) {
            var request = context.Request;
            var url     = request.Url;
            var path    = url.LocalPath;
            var method  = request.HttpMethod;
            if (!HttpHostUtils.GetFlioxRoute(httpHost, path, out string route)) {
                return HttpHostUtils.ExecuteUnknownPath(httpHost, path, method);
            }
            HttpListenerRequest req  = context.Request;
            WebSocket           websocket = null;

            if (httpHost.AcceptWebSocketType == AcceptWebSocketType.FrifloWebSockets) {
                // Unity <= 2021.3 has currently no support for Server WebSockets  =>  add functionality using ServerWebSocketExtensions
                // see: [Help Wanted - Websocket Server in Standalone build - Unity Forum] https://forum.unity.com/threads/websocket-server-in-standalone-build.1072526/
                if (ServerWebSocketExtensions.IsWebSocketRequest(req)) {
                    var wsContext   = await ServerWebSocketExtensions.AcceptWebSocket(context).ConfigureAwait(false);
                    websocket       = wsContext.WebSocket;
                }
            }
            else
            {
                if (req.IsWebSocketRequest) {
                    var wsContext   = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                    websocket       = wsContext.WebSocket;
                }
            }
            if (websocket != null) {
                var remoteClient  = request.RemoteEndPoint;
                // awaits until thew websocket is closed or disconnected
                await WebSocketHost.SendReceiveMessages (websocket, remoteClient, httpHost).ConfigureAwait(false);
                
                return null;
            }
            var headers         = new HttpListenerHeaders(request.Headers, request.Cookies);
            var contentLength   = (int)req.ContentLength64;
            using(var memoryBuffer = httpHost.sharedEnv.MemoryBuffer.Get()) {
                var requestContext  = new RequestContext(httpHost, method, route, url.Query, req.InputStream, contentLength, headers, memoryBuffer.instance);
                await httpHost.ExecuteHttpRequest(requestContext).ConfigureAwait(false);
                
                return requestContext;
            }
        }
        
        /// <summary>
        /// Write the result of <see cref="ExecuteFlioxRequest"/> to the given <paramref name="context"/>
        /// </summary>
        public static async Task WriteFlioxResponse(this HttpListenerContext context, RequestContext cx) {
            if (cx == null)
                return; // request was WebSocket
            HttpListenerResponse resp = context.Response;
            if (!cx.Handled) {
                var body = $"{context.Request.Url} not found";
                await resp.WriteResponseString("text/plain", 404, body, null).ConfigureAwait(false);
                return;
            }
            var respBody = cx.Response;
            if (respBody.Count < 4094 || cx.ResponseGzip) {
                SetResponseHeader(resp, cx.ResponseContentType, cx.StatusCode, respBody.Count, cx.ResponseHeaders, cx.ResponseGzip);
                await resp.OutputStream.WriteAsync(respBody).ConfigureAwait(false);
            } else {
                SetResponseHeaderGZip(resp, cx.ResponseContentType, cx.StatusCode, cx.ResponseHeaders);
                using GZipStream zipStream = new GZipStream(resp.OutputStream, CompressionMode.Compress, false);
                await zipStream.WriteAsync(respBody.MutableArray, 0, respBody.Count).ConfigureAwait(false);
            }
            resp.Close();
        }
        
        public static async Task WriteResponseString (this HttpListenerResponse response, string contentType, int statusCode, string value, Dictionary<string, string> headers) {
            byte[]  resultBytes = Encoding.UTF8.GetBytes(value);
            
            SetResponseHeader(response, contentType, statusCode, resultBytes.Length, headers, false);
            await response.OutputStream.WriteAsync(resultBytes, 0, resultBytes.Length).ConfigureAwait(false);
        }
        
        private static void SetResponseHeader (
            HttpListenerResponse        response,
            string                      contentType,
            int                         statusCode,
            int                         len,
            Dictionary<string, string>  headers,
            bool                        gzip)
        {
            response.ContentType        = contentType;
            response.ContentEncoding    = Encoding.UTF8;
            response.ContentLength64    = len;
            response.StatusCode         = statusCode;
            if (gzip) {
                response.AddHeader("Content-Encoding", "gzip");
            }
        //  resp.Headers["link"]    = "rel=\"icon\" href=\"#\""; // not working as expected - expect no additional request of favicon.ico
            if (headers != null) {
                foreach (var header in headers) {
                    response.Headers[header.Key] = header.Value;
                }
            }
        }
        
        private static void SetResponseHeaderGZip (HttpListenerResponse response, string contentType, int statusCode, Dictionary<string, string> headers) {
            response.ContentType        = contentType;
            response.StatusCode         = statusCode;
            response.AddHeader("Content-Encoding", "gzip");
            //  resp.Headers["link"]    = "rel=\"icon\" href=\"#\""; // not working as expected - expect no additional request of favicon.ico
            if (headers != null) {
                foreach (var header in headers) {
                    response.Headers[header.Key] = header.Value;
                }
            }
        }
    }
    
    internal sealed class HttpListenerHeaders : IHttpHeaders {
        private  readonly   NameValueCollection headers;
        private  readonly   CookieCollection    cookies;
        
        public              string              Header(string key) => headers[key];
        public              string              Cookie(string key) => cookies[key]?.Value;
        
        internal HttpListenerHeaders(NameValueCollection headers, CookieCollection cookies) {
            this.headers = headers;
            this.cookies = cookies;
        }
    }
    
}
