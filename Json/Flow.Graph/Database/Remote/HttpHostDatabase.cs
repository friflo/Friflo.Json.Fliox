// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
{
    
    public interface IHttpContextHandler
    {
        Task<bool> HandleContext(HttpListenerContext context);
    }
    
    // [A Simple HTTP server in C#] https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
    //
    // Note:
    // Alternatively a HTTP web server could be implemented by using Kestrel.
    // See: [Deprecate HttpListener · Issue #88 · dotnet/platform-compat] https://github.com/dotnet/platform-compat/issues/88#issuecomment-592395933
    // See: [Configure options for the ASP.NET Core Kestrel web server | Microsoft Docs] https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options?view=aspnetcore-5.0
    public class HttpHostDatabase : RemoteHostDatabase
    {
        private readonly    string              endpoint;
        private readonly    HttpListener        listener;
        private readonly    IHttpContextHandler contextHandler;
        private             bool                runServer;
        
        private             int                 requestCount;

        
        public HttpHostDatabase(EntityDatabase local, string endpoint, IHttpContextHandler contextHandler) : base(local) {
            this.endpoint       = endpoint;
            this.contextHandler = contextHandler;
            listener            = new HttpListener();
            listener.Prefixes.Add(endpoint);
        }
        
        private async Task HandleIncomingConnections()
        {
            runServer = true;

            while (runServer) {
                try {
                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync().ConfigureAwait(false);
                    // await HandleListenerContext(ctx);            // handle incoming requests serial
                    _ = Task.Run(async () => {
                        try {
                            await HandleListenerContext(ctx); // handle incoming requests parallel
                        }
                        catch (Exception e) {
                            Log($"Request failed - {e.GetType()}: {e.Message}");
                        }
                    });
                }
#if UNITY_5_3_OR_NEWER
                catch (ObjectDisposedException  e) {
                    if (runServer)
                        Log($"RemoteHost error: {e}");
                    return;
                }
#endif
                catch (HttpListenerException  e) {
                    bool serverStopped = e.ErrorCode == 995 && runServer == false;
                    if (!serverStopped) 
                        Log($"RemoteHost error: {e}");
                    return;
                }
                catch (Exception e) {
                     Log($"RemoteHost error: {e}");
                     return;
                }
            }
        }
        
        private async Task HandleListenerContext (HttpListenerContext ctx) {
            HttpListenerRequest  req  = ctx.Request;
            HttpListenerResponse resp = ctx.Response;

            if (requestCount++ == 0 || requestCount % 500 == 0) {
                string reqMsg = $@"request {requestCount} {req.Url} {req.HttpMethod} {req.UserAgent}"; // {req.UserHostName} 
                Log(reqMsg);
            }
            if (req.IsWebSocketRequest) {
                await AcceptWebSocket (ctx).ConfigureAwait(false);
                return;
            }

            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/") {
                var inputStream = req.InputStream;
                StreamReader reader = new StreamReader(inputStream, Encoding.UTF8);
                string requestContent = await reader.ReadToEndAsync().ConfigureAwait(false);

                var contextPools    = new Pools(Pools.SharedPools);
                var syncContext     = new SyncContext(contextPools, null);
                var     result      = await ExecuteRequestJson(requestContent, syncContext, ResponseType.Response).ConfigureAwait(false);
                syncContext.pools.AssertNoLeaks();
                byte[]  resultBytes = Encoding.UTF8.GetBytes(result.body);
                HttpStatusCode statusCode;
                switch (result.statusType){
                    case RequestStatusType.Ok:          statusCode = HttpStatusCode.OK;                     break;
                    case RequestStatusType.Error:       statusCode = HttpStatusCode.BadRequest;             break;
                    case RequestStatusType.Exception:   statusCode = HttpStatusCode.InternalServerError;    break;
                    default:                            statusCode = HttpStatusCode.InternalServerError;    break;
                } 
                SetResponseHeader(resp, "application/json", statusCode, resultBytes.Length);
                await resp.OutputStream.WriteAsync(resultBytes, 0, resultBytes.Length).ConfigureAwait(false);
                resp.Close();
                return;
            }
            if (contextHandler != null) {
                var success = await contextHandler.HandleContext(ctx).ConfigureAwait(false);
                if (success)
                    return;
            }
            var     response        = $"invalid url: {req.Url}, method: {req.HttpMethod}";
            byte[]  responseBytes   = Encoding.UTF8.GetBytes(response);
            SetResponseHeader(resp, "text/plain", HttpStatusCode.BadRequest, responseBytes.Length);
            await resp.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
            resp.Close();
        }
        
        private async Task AcceptWebSocket(HttpListenerContext ctx) {
            var         wsContext   = await ctx.AcceptWebSocketAsync(null);
            WebSocket   websocket   = wsContext.WebSocket;
            var         buffer      = new ArraySegment<byte>(new byte[8192]);
            WebSocketTarget target  = new WebSocketTarget(websocket);
            using (var memoryStream = new MemoryStream()) {
                while (true) {
                    switch (websocket.State) {
                        case WebSocketState.Open:
                            memoryStream.Position = 0;
                            memoryStream.SetLength(0);
                            WebSocketReceiveResult wsResult;
                            do {
                                wsResult = await websocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                                memoryStream.Write(buffer.Array, buffer.Offset, wsResult.Count);
                            }
                            while(!wsResult.EndOfMessage);
                            
                            if (wsResult.MessageType == WebSocketMessageType.Text) {
                                var requestContent  = Encoding.UTF8.GetString(memoryStream.ToArray());
                                var contextPools    = new Pools(Pools.SharedPools);
                                var syncContext     = new SyncContext(contextPools, target);
                                var result          = await ExecuteRequestJson(requestContent, syncContext, ResponseType.Message).ConfigureAwait(false);
                                syncContext.pools.AssertNoLeaks();
                                byte[] resultBytes  = Encoding.UTF8.GetBytes(result.body);
                                var arraySegment    = new ArraySegment<byte>(resultBytes, 0, resultBytes.Length);
                                await websocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                            }
                            break;
                        case WebSocketState.CloseReceived:
                            Log("WebSocket CloseReceived");
                            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            return;
                        case WebSocketState.Closed:
                            Log("WebSocket Closed");
                            return;
                        case WebSocketState.Aborted:
                            Log("WebSocket Aborted");
                            return;
                    }
                }
            }
        }

        public static void SetResponseHeader (HttpListenerResponse resp, string contentType, HttpStatusCode statusCode, int len) {
            resp.ContentType        = contentType;
            resp.ContentEncoding    = Encoding.UTF8;
            resp.ContentLength64    = len;
            resp.StatusCode         = (int)statusCode;
        }

        // Http server requires setting permission to run an http server.
        // Otherwise exception is thrown on startup: System.Net.HttpListenerException: permission denied.
        // To give access see: [add urlacl - Win32 apps | Microsoft Docs] https://docs.microsoft.com/en-us/windows/win32/http/add-urlacl
        //     netsh http add urlacl url=http://+:8081/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl  http://+:8081/
        // 
        // Get DOMAIN\USER via  PowerShell
        //     $env:UserName
        //     $env:UserDomain
        //
        public void Start() {
            // Create a Http server and start listening for incoming connections
            listener.Start();
            Log($"Listening for connections on {this.endpoint}");
        }

        public void Run() {
            // Handle requests
            var listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
        }
        
        public async Task Stop() {
            await Task.Delay(1).ConfigureAwait(false);
            runServer = false;
            listener.Stop();
        }


        private void Log(string msg) {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif
        }
    }
    
    internal class WebSocketTarget : IEventTarget
    {
        readonly WebSocket webSocket;
        
        internal WebSocketTarget (WebSocket webSocket) {
            this.webSocket = webSocket;
        }
        
        public bool IsOpen () {
            return webSocket.State == WebSocketState.Open;
        }
        
        public async Task<bool> SendEvent(DatabaseEvent ev, SyncContext syncContext) {
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                var writer = pooledMapper.instance.writer;
                writer.WriteNullMembers = false;
                writer.Pretty           = true;
                var message         = new WebSocketMessage { ev = ev };
                var jsonMessage     = writer.Write(message);
                byte[] jsonBytes    = Encoding.UTF8.GetBytes(jsonMessage);
                try {
                    var arraySegment    = new ArraySegment<byte>(jsonBytes, 0, jsonBytes.Length);
                    await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                    return true;
                }
                catch (Exception) {
                    return false;
                }
            }
        }
    }
}