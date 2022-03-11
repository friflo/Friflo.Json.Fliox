// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Remote
{
    // [A Simple HTTP server in C#] https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
    //
    // Note:
    // Alternatively a HTTP web server could be implemented by using Kestrel.
    // See: [Deprecate HttpListener · Issue #88 · dotnet/platform-compat] https://github.com/dotnet/platform-compat/issues/88#issuecomment-592395933
    // See: [Configure options for the ASP.NET Core Kestrel web server | Microsoft Docs] https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options?view=aspnetcore-5.0
    /// <summary>
    /// <see cref="HttpListenerHost"/> is a utility class used to <br/>
    ///  - handle Fliox requests <br/>
    ///  - manage the lifecycle of a <see cref="HttpListener"/>. <br/>
    /// <br/>
    /// lifecycle methods:
    /// <list type="bullet">
    ///     <item>Create an instance: <see cref="HttpListenerHost(string, HttpHostHub)"/></item>
    ///     <item>Start server: <see cref="Start"/></item>
    ///     <item>Run server loop for incoming connections: <see cref="Run"/></item>
    ///     <item>Stop server: <see cref="Stop"/></item>
    ///     <item>Shutdown server: <see cref="Dispose"/></item>
    /// </list> 
    /// </summary>
    public sealed class HttpListenerHost : IDisposable
    {
        private  readonly   HttpListener        listener;
        private             bool                runServer;
        private             int                 requestCount;
        private  readonly   HttpHostHub         hostHub;
        
        public HttpListenerHost(HttpListener httpListener, HttpHostHub hostHub) {
            this.hostHub    = hostHub;
            listener        = httpListener;
        }
        
        public HttpListenerHost(string endpoint, HttpHostHub hostHub)
            : this (CreateHttpListener(new []{endpoint}), hostHub)
        { }
        
        private static HttpListener CreateHttpListener(string[] endpoints) {
            var listener = new HttpListener();
            foreach (var endpoint in endpoints) {
                listener.Prefixes.Add(endpoint);
            }
            return listener;
        }

        public void Dispose() {
            listener.Close();
        }

        private async Task HandleIncomingConnections()
        {
            runServer = true;

            while (runServer) {
                try {
                    // Will wait here until we hear from a connection
                    HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);
                    // await HandleListenerContext(context);            // handle incoming requests serial
                    _ = Task.Run(async () => {
                        try {
                            HttpListenerRequest  req  = context.Request;
                            if (requestCount++ == 0 || requestCount % 10000 == 0) {
                                string reqMsg = $@"request {requestCount} {req.Url} {req.HttpMethod} {req.UserAgent}"; // {req.UserHostName} 
                                Log(reqMsg);
                            }
                            var response = await HttpListenerUtils.ExecuteFlioxRequest(context, hostHub).ConfigureAwait(false); // handle incoming requests parallel
                            
                            await HttpListenerUtils.WriteFlioxResponse(context, response).ConfigureAwait(false);
                        }
                        catch (Exception e) {
                            await HandleContextException(context, e);
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
        
        private async Task HandleContextException(HttpListenerContext context, Exception e) {
            var message = $"request failed - {e.GetType().Name}: {e.Message}";
            Log(message);
            var resp    = context.Response;
            if (!resp.OutputStream.CanWrite)
                return;
            byte[]  responseBytes   = Encoding.UTF8.GetBytes(message);
            HttpListenerUtils.SetResponseHeader(resp, "text/plain", (int)HttpStatusCode.BadRequest, responseBytes.Length, null);
            await resp.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
            resp.Close();
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
            var sb = new StringBuilder();
            sb.Append("Listening for connections on:");
            foreach (var prefix in listener.Prefixes) {
                sb.Append(" ");
                sb.Append(prefix);
            }
            Log(sb.ToString());
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
