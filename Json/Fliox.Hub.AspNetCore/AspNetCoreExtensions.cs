// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
#if !UNITY_2020_1_OR_NEWER

using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

// Assembly "Fliox.Hub.AspNetCore" uses the 'floating version dependency':
//      <PackageReference Include="Microsoft.AspNetCore.Http" Version="*" />
//
// More info for 'floating version dependency':
// [NuGet Package Dependency Resolution | Microsoft Docs] https://docs.microsoft.com/en-us/nuget/concepts/dependency-resolution#floating-versions
namespace Friflo.Json.Fliox.Hub.AspNetCore
{
    public static class AspNetCoreExtensions
    {
        /// <summary>
        /// Execute the request and write the response to the given <paramref name="context"/>.
        /// </summary>
        /// <remarks>
        /// It a shortcut for
        /// <code>
        ///     var response = await context.ExecuteFlioxRequest(httpHost).ConfigureAwait(false);
        ///     await context.WriteFlioxResponse(response).ConfigureAwait(false);
        /// </code>
        /// </remarks>
        public static async Task HandleFlioxRequest(this HttpContext context, HttpHost httpHost) {
            var requestContext = await ExecuteFlioxRequest(context, httpHost).ConfigureAwait(false);
            await WriteFlioxResponse(context, requestContext).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Execute the request return a RequestContext containing the execution result.
        /// To return a HTTP response <see cref="WriteFlioxResponse"/> need to be called. 
        /// </summary>
        public static async Task<RequestContext> ExecuteFlioxRequest(this HttpContext context, HttpHost httpHost) {
            var httpRequest = context.Request;
            var path        = httpRequest.Path.Value;
            if (!httpHost.GetRoute(path, out string route)) {
                return httpHost.InternalRequestError(path, httpRequest.Method);
            }
            var isWebSocket = context.WebSockets.IsWebSocketRequest;
            if (isWebSocket) {
                WebSocket ws        = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                var httpConnection  = context.Features.Get<IHttpConnectionFeature>();
                var remoteEndPoint  = new IPEndPoint(httpConnection.RemoteIpAddress, httpConnection.RemotePort);
                await WebSocketHost.SendReceiveMessages(ws, remoteEndPoint, httpHost).ConfigureAwait(false);
                return null;
            }
            var headers     = new HttpContextHeaders(httpRequest.Headers);
            var cookies     = new HttpContextCookies(httpRequest.Cookies);
            var reqCtx      = new RequestContext(httpHost, httpRequest.Method, route, httpRequest.QueryString.Value, httpRequest.Body, headers, cookies);

            await httpHost.ExecuteHttpRequest(reqCtx).ConfigureAwait(false);
                    
            return reqCtx;
        }
        
        /// <summary>
        /// Write the result of <see cref="ExecuteFlioxRequest"/> to the given <paramref name="context"/>
        /// </summary>
        public static async Task WriteFlioxResponse(this HttpContext context, RequestContext requestContext) {
            if (requestContext == null)
                return; // request was WebSocket
            var httpResponse            = context.Response;
            JsonValue response          = requestContext.Response;
            httpResponse.StatusCode     = requestContext.StatusCode;
            httpResponse.ContentType    = requestContext.ResponseContentType;
            httpResponse.ContentLength  = response.Length;
            var responseHeaders         = requestContext.ResponseHeaders;
            if (responseHeaders != null) {
                foreach (var header in responseHeaders) {
                    httpResponse.Headers[header.Key] = header.Value;
                }
            }
            await httpResponse.Body.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Return the start page url intended to be written to the console to simplify debugging.<br/>
        /// Like: <c>http://localhost:8010/fliox/</c>
        /// </summary>
        /// <param name="httpHost">used to get the path of the start page url</param>
        /// <param name="addresses">used to get the port of the start page url</param>
        public static string GetStartPage(this HttpHost httpHost, ICollection<string> addresses) {
            foreach (var address in addresses) {
                var portPos = address.LastIndexOf(':');
                if (portPos != -1) {
                    var port = address.Substring(portPos + 1);
                    return $"http://localhost:{port}{httpHost.endpoint}";
                }
            }
            return $"http://localhost{httpHost.endpoint}";
        }
    }
    
    internal class HttpContextHeaders : IHttpHeaders {
        private readonly    IHeaderDictionary   headers;
        
        public              string              this[string key] => headers[key];
        
        internal HttpContextHeaders(IHeaderDictionary headers) {
            this.headers = headers;    
        }
    }
    
    internal class HttpContextCookies : IHttpCookies {
        private readonly    IRequestCookieCollection    cookies;
        
        public              string              this[string key] => cookies[key];
        
        internal HttpContextCookies(IRequestCookieCollection headers) {
            this.cookies = headers;    
        }
    }
}

#endif