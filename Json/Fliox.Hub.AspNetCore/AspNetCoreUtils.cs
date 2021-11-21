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
        public static async Task HandleFlioxHostRequest(this HttpContext context, HttpHostHub hostHub) {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                await WebSocketHost.SendReceiveMessages(ws, hostHub).ConfigureAwait(false);
                return;
            }
            var httpRequest = context.Request;
            var headers     = new HttpContextHeaders(httpRequest.Headers);
            var cookies     = new HttpContextCookies(httpRequest.Cookies);
            var reqCtx = new RequestContext(httpRequest.Method, httpRequest.Path.Value, httpRequest.QueryString.Value, httpRequest.Body, headers, cookies);
            await hostHub.ExecuteHttpRequest(reqCtx).ConfigureAwait(false);
                    
            var httpResponse            = context.Response;
            JsonValue response          = reqCtx.Response;
            httpResponse.StatusCode     = reqCtx.StatusCode;
            httpResponse.ContentType    = reqCtx.ResponseContentType;
            httpResponse.ContentLength  = response.Length;
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