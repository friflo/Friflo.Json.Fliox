// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
#if !UNITY_2020_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

// Assembly "Fliox.Hub.AspNetCore" uses the 'floating version dependency':
//      <PackageReference Include="Microsoft.AspNetCore.Http" Version="*" />
//
// More info for 'floating version dependency':
// [NuGet Package Dependency Resolution | Microsoft Docs] https://docs.microsoft.com/en-us/nuget/concepts/dependency-resolution#floating-versions
namespace Friflo.Json.Fliox.Hub.AspNetCore
{
    public static class AspNetCoreExtensions
    {
        /* public static IWebHostBuilder UseFlioxHost(this IWebHostBuilder hostBuilder, HttpHost httpHost) {
            hostBuilder.ConfigureServices(services => services.AddSingleton(httpHost));
            return hostBuilder;
        } */
        
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
            if (!HttpHostUtils.GetFlioxRoute(httpHost, path, out string route)) {
                return HttpHostUtils.ExecuteUnknownPath(httpHost, path, httpRequest.Method);
            }
            var isWebSocket = context.WebSockets.IsWebSocketRequest;
            if (isWebSocket) {
                var websocket       = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                var httpConnection  = context.Features.Get<IHttpConnectionFeature>();
                var remoteClient    = new IPEndPoint(httpConnection.RemoteIpAddress, httpConnection.RemotePort);
                // awaits until the websocket is closed or disconnected
                await WebSocketHost.SendReceiveMessages(websocket, remoteClient, httpHost).ConfigureAwait(false);
                
                return null;
            }
            var headers         = new HttpContextHeaders(httpRequest.Headers, httpRequest.Cookies);
            var query           = httpRequest.QueryString.Value;
            var body            = httpRequest.Body;
            int bodyLength      = (int)(httpRequest.ContentLength ?? 0);
            using(var memoryBuffer = httpHost.sharedEnv.MemoryBuffer.Get()) {
                var requestContext  = new RequestContext(httpHost, httpRequest.Method, route, query, body, bodyLength, headers, memoryBuffer.instance);
                await httpHost.ExecuteHttpRequest(requestContext).ConfigureAwait(false);
                    
                return requestContext;
            }
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
            httpResponse.ContentLength  = response.Count;
            var responseHeaders         = requestContext.ResponseHeaders;
            if (responseHeaders != null) {
                foreach (var header in responseHeaders) {
                    httpResponse.Headers[header.Key] = header.Value;
                }
            }
            await httpResponse.Body.WriteAsync(response).ConfigureAwait(false);
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
                    return $"http://localhost:{port}{httpHost.baseRoute}";
                }
            }
            return $"http://localhost{httpHost.baseRoute}";
        }
        
        /// <summary>
        /// Return the start page url intended to be written to the console to simplify debugging.<br/>
        /// Like: <c>http://localhost:8010/fliox/</c>
        /// </summary>
        public static string GetStartPage(this HttpHost httpHost, IServiceProvider services) {
            var addresses = services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
            return GetStartPage(httpHost, addresses);
        }
    }
    
    internal sealed class HttpContextHeaders : IHttpHeaders {
        private readonly    IHeaderDictionary           headers;
        private readonly    IRequestCookieCollection    cookies;
        
        public              string              Header(string key) => headers[key];
        public              string              Cookie(string key) => cookies[key];
        
        internal HttpContextHeaders(IHeaderDictionary headers, IRequestCookieCollection cookies) {
            this.headers = headers;
            this.cookies = cookies;
        }
    }
}

#endif