#if !UNITY_2020_1_OR_NEWER

using System.Net.WebSockets;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Microsoft.AspNetCore.Http;

// Assembly "Fliox.Hub.AspNetCore" uses the 'floating version dependency':
//      <PackageReference Include="Microsoft.AspNetCore.Http" Version="*" />
//
// More info for 'floating version dependency':
// [NuGet Package Dependency Resolution | Microsoft Docs] https://docs.microsoft.com/en-us/nuget/concepts/dependency-resolution#floating-versions
namespace Friflo.Json.Fliox.Hub.AspNetCore
{
    public static class AspNetCoreUtils
    {
        public static async Task<RequestContext> HandleFlioxHostRequest(this HttpContext context, HttpHostHub hostHub) {
            var isWebSocket = context.WebSockets.IsWebSocketRequest;
            if (isWebSocket) {
                WebSocket ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                await WebSocketHost.SendReceiveMessages(ws, hostHub).ConfigureAwait(false);
                return null;
            }
            var httpRequest = context.Request;
            var headers     = new HttpContextHeaders(httpRequest.Headers);
            var cookies     = new HttpContextCookies(httpRequest.Cookies);
            var reqCtx      = new RequestContext(httpRequest.Method, httpRequest.Path.Value, httpRequest.QueryString.Value, httpRequest.Body, headers, cookies);

            await hostHub.ExecuteHttpRequest(reqCtx).ConfigureAwait(false);
                    
            return reqCtx;
        }
        
        public static async Task HandleFlioxHostResponse(this HttpContext context, RequestContext requestContext) {
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