// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    
    public interface IHttpContextHandler
    {
        Task<bool> HandleContext(HttpListenerContext context, HttpHostDatabase hostDatabase);
    }
    
    // [A Simple HTTP server in C#] https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
    //
    // Note:
    // Alternatively a HTTP web server could be implemented by using Kestrel.
    // See: [Deprecate HttpListener · Issue #88 · dotnet/platform-compat] https://github.com/dotnet/platform-compat/issues/88#issuecomment-592395933
    // See: [Configure options for the ASP.NET Core Kestrel web server | Microsoft Docs] https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options?view=aspnetcore-5.0
    public class HttpHostDatabase : RemoteHostDatabase
    {
        public              IHttpContextHandler schemaHandler = new SchemaHandler();
        
        private  readonly   string              endpoint;
        private  readonly   HttpListener        listener;
        private  readonly   IHttpContextHandler contextHandler;
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
                            await HandleListenerContext(ctx).ConfigureAwait(false); // handle incoming requests parallel
                        }
                        catch (Exception e) {
                            Log($"Request failed - {e.GetType()}: {e.Message}");
                            var     req             = ctx.Request;
                            var     resp            = ctx.Response;
                            var     response        = $"invalid url: {req.Url}, method: {req.HttpMethod}";
                            byte[]  responseBytes   = Encoding.UTF8.GetBytes(response);
                            SetResponseHeader(resp, "text/plain", HttpStatusCode.BadRequest, responseBytes.Length);
                            await resp.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                            resp.Close();
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
        
        private static async Task HandleServerWebSocket (HttpListenerResponse resp) {
            const string error = "Unity HttpListener doesnt support server WebSockets";
            byte[]  resultBytes = Encoding.UTF8.GetBytes(error);
            SetResponseHeader(resp, "text/plain", HttpStatusCode.NotImplemented, resultBytes.Length);
            await resp.OutputStream.WriteAsync(resultBytes, 0, resultBytes.Length).ConfigureAwait(false);
            resp.Close();
        }
        
        private async Task HandleListenerContext (HttpListenerContext ctx) {
            HttpListenerRequest  req  = ctx.Request;
            HttpListenerResponse resp = ctx.Response;

            if (requestCount++ == 0 || requestCount % 10000 == 0) {
                string reqMsg = $@"request {requestCount} {req.Url} {req.HttpMethod} {req.UserAgent}"; // {req.UserHostName} 
                Log(reqMsg);
            }
            // accepting WebSockets in Unity fails at IsWebSocketRequest. See: 
            // [Help Wanted - Websocket Server in Standalone build - Unity Forum] https://forum.unity.com/threads/websocket-server-in-standalone-build.1072526/
            //
            // Possible solutions may be like:
            // (MIT License) [ninjasource/Ninja.WebSockets: A c# implementation of System.Net.WebSockets.WebSocket for .Net Standard 2.0] https://github.com/ninjasource/Ninja.WebSockets
            // (MIT License) [sta/websocket-sharp: A C# implementation of the WebSocket protocol client and server] https://github.com/sta/websocket-sharp
#if UNITY_5_3_OR_NEWER
            if (req.Headers["Connection"] == "Upgrade" && req.Headers["Upgrade"] != null) {
                await HandleServerWebSocket(resp).ConfigureAwait(false);
                return;
            }
#endif
            if (req.IsWebSocketRequest) {
                await WebSocketHostTarget.AcceptWebSocket (ctx, this).ConfigureAwait(false);
                return;
            }

            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/") {
                var inputStream     = req.InputStream;
                var requestContent  = await JsonUtf8.ReadToEndAsync(inputStream).ConfigureAwait(false);

                // Each request require its own pool as multiple request running concurrently. Could cache a Pools instance per connection.
                var pools           = new Pools(Pools.SharedPools);
                var messageContext  = new MessageContext(pools, null);
                var result          = await ExecuteRequestJson(requestContent, messageContext).ConfigureAwait(false);
                messageContext.Release();
                var  body = result.body;
                HttpStatusCode statusCode;
                switch (result.statusType){
                    case ResponseStatusType.Ok:         statusCode = HttpStatusCode.OK;                     break;
                    case ResponseStatusType.Error:      statusCode = HttpStatusCode.BadRequest;             break;
                    case ResponseStatusType.Exception:  statusCode = HttpStatusCode.InternalServerError;    break;
                    default:                            statusCode = HttpStatusCode.InternalServerError;    break;
                } 
                SetResponseHeader(resp, "application/json", statusCode, body.Length);
                await resp.OutputStream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
                resp.Close();
                return;
            }
            bool success = await schemaHandler.HandleContext(ctx, this).ConfigureAwait(false);
            if (success)
                return;
            contextHandler?.HandleContext(ctx, this).ConfigureAwait(false);
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
        //     netsh http add urlacl url=http://+:8010/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl  http://+:8010/
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
    

}