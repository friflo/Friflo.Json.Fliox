#if !UNITY_2020_1_OR_NEWER

using System.Net.WebSockets;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;
using Microsoft.AspNetCore.Http;

// Assembly "Fliox.DB.AspNetCore" uses the 'floating version dependency':
//      <PackageReference Include="Microsoft.AspNetCore.Http" Version="*" />
//
// More info for 'floating version dependency':
// [NuGet Package Dependency Resolution | Microsoft Docs] https://docs.microsoft.com/en-us/nuget/concepts/dependency-resolution#floating-versions
namespace Friflo.Json.Fliox.DB.AspNetCore
{
    public static class AspNetCoreUtils
    {
        public static async Task HandleFlioxHostRequest(this HttpContext context, HttpHostHub hostHub) {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
                await WebSocketHost.SendReceiveMessages(ws, hostHub);
                return;
            }
            var httpRequest = context.Request;
            var reqCtx = new RequestContext(httpRequest.Method, httpRequest.Path.Value, httpRequest.Body);
            await hostHub.ExecuteHttpRequest(reqCtx).ConfigureAwait(false);
                    
            var httpResponse            = context.Response;
            JsonUtf8 response           = reqCtx.Response;
            httpResponse.StatusCode     = reqCtx.StatusCode;
            httpResponse.ContentType    = reqCtx.ResponseContentType;
            httpResponse.ContentLength  = response.Length;
            await httpResponse.Body.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
        }
    }
}

#endif