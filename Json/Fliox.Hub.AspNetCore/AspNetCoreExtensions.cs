// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
#if !UNITY_2020_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        /// Adds a redirect to the <b>Hub Explorer</b> of <paramref name="httpHost"/>
        /// to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests for the specified pattern.
        /// </summary>
        public static void MapRedirect(this IEndpointRouteBuilder app, string pattern, HttpHost httpHost) {
            app.MapGet(pattern, async context => {
                context.Response.Redirect(httpHost.baseRoute, false);
                await context.Response.WriteAsync("redirect").ConfigureAwait(false);
            });
        }
        
        /// <summary>
        /// Adds the <paramref name="httpHost"/> as a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        public static void MapHost(this IEndpointRouteBuilder app, string pattern, HttpHost httpHost) {
            app.Map(pattern, async context =>  {
                var requestContext = await context.ExecuteFlioxRequest(httpHost).ConfigureAwait(false);
                await context.WriteFlioxResponse(requestContext).ConfigureAwait(false);
            });
        }
        
        /// <summary>
        /// Same behavior as <c>app.Run();</c><br/>
        /// Writes the base URL of the <see cref="HttpHost"/> to console log.
        /// </summary>
        public static void RunLogUrl(this IHost app, HttpHost httpHost) {
            app.Start();
            Console.WriteLine($"Hub Explorer - {httpHost.GetStartPage(app.Services)}\n");
            app.WaitForShutdown();
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
            if (requestContext == null) {
                return; // request was WebSocket
            }
            var httpResponse            = context.Response;
            JsonValue response          = requestContext.Response;
            httpResponse.StatusCode     = requestContext.StatusCode;
            httpResponse.ContentType    = requestContext.ResponseContentType;
            var responseHeaders         = requestContext.ResponseHeaders;
            if (responseHeaders != null) {
                foreach (var header in responseHeaders) {
                    httpResponse.Headers[header.Key] = header.Value;
                }
            }
            bool raw = response.Count < 4094 || requestContext.ResponseGzip;
            if (raw) {
                httpResponse.ContentLength  = response.Count;
                if (requestContext.ResponseGzip) {
                    httpResponse.Headers["Content-Encoding"] = "gzip";
                }
                await httpResponse.Body.WriteAsync(response).ConfigureAwait(false);
            } else {
                httpResponse.Headers["Content-Encoding"] = "gzip";
                await using var zipStream = new GZipStream(httpResponse.Body, CompressionMode.Compress, false);
                await zipStream.WriteAsync(response.AsReadOnlyMemory()).ConfigureAwait(false);
            }
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
        
        public static void UseAspNetCoreLogger(this HttpHost httpHost, IServiceProvider services) {
            var loggerFactory = services.GetService<ILoggerFactory>();
            httpHost.sharedEnv.Logger = new HubLoggerAspNetCore(loggerFactory);
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