// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <see cref="HttpListenerHost"/> is a utility class to enable running a simple HTTP Server by using a <see cref="HttpListener"/>
    /// </summary>
    /// <remarks>
    /// Via its utility methods is manages the lifecycle of a <see cref="HttpListener"/>.
    /// Lifecycle methods:
    /// <list type="bullet">
    ///     <item>Create an instance: <see cref="HttpListenerHost(string, HttpHost)"/></item>
    ///     <item>Start server: <see cref="Start"/></item>
    ///     <item>Run server loop for incoming connections: <see cref="Run"/></item>
    ///     <item>Stop server: <see cref="Stop"/></item>
    ///     <item>Shutdown server: <see cref="Dispose"/></item>
    /// </list>
    /// </remarks>
    public sealed class HttpListenerHost : IDisposable, ILogSource
    {
        private  readonly   HttpListener                    listener;
        private             bool                            runServer;
        private             int                             requestCount;
        private  readonly   HttpHost                        httpHost;
        public              Func<HttpListenerContext, Task> customRequestHandler;
        
        public   override   string                          ToString() => $"endpoint: {httpHost.endpoint}";
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger                      Logger { get; }
        
        public HttpListenerHost(HttpListener httpListener, HttpHost httpHost) {
            this.httpHost           = httpHost;
            listener                = httpListener;
            Logger                  = httpHost.sharedEnv.hubLogger;
            customRequestHandler    = ExecuteRequest;
        }
        
        public HttpListenerHost(string endpoint, HttpHost httpHost)
            : this (CreateHttpListener(new []{endpoint}), httpHost)
        { }
        
        // Note: Http server may require a permission to listen to the given host/port on Windows.
        // Otherwise exception is thrown on startup: System.Net.HttpListenerException: permission denied.
        // To give access see: [add urlacl - Win32 apps | Microsoft Docs] https://docs.microsoft.com/en-us/windows/win32/http/add-urlacl
        //     netsh http add urlacl url=http://+:8010/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl http://+:8010/
        // Get DOMAIN\USER via  PowerShell > $env:UserName / $env:UserDomain 
        public static int RunHost(string endpoint, HttpHost httpHost) {
            var server = new HttpListenerHost(endpoint, httpHost);
            server.Start();
            server.Run();
            return 0;
        }
        
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
            runServer       = true;

            while (runServer) {
                try {
                    await HandleRequest().ConfigureAwait(false);
                }
#if UNITY_5_3_OR_NEWER
                catch (ObjectDisposedException  e) {
                    if (runServer)
                        LogException("HttpListenerHost - ObjectDisposedException", e);
                    return;
                }
#endif
                catch (HttpListenerException  e) {
                    bool serverStopped = e.ErrorCode == 995 && runServer == false;
                    if (!serverStopped) 
                        LogException("HttpListenerHost - HttpListenerException", e);
                    return;
                }
                catch (Exception e) {
                     LogException("HttpListenerHost - Exception", e);
                     return;
                }
            }
        }
        
        private bool IsFlioxRequest (string path) {
            if (path.StartsWith(httpHost.endpoint)) {
                return true;
            }
            return path == httpHost.endpointRoot;
        }
        
        private async Task HandleRequest()
        {
            // Will wait here until we hear from a connection
            HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);

            _ = Task.Run(async () => {
                HttpListenerRequest  req  = context.Request;
                if (requestCount++ == 0 || requestCount % 10000 == 0) {
                    string reqMsg = $@"request {requestCount} {req.Url} {req.HttpMethod}"; // {req.UserAgent} {req.UserHostName} 
                    LogInfo(reqMsg);
                }
                var path = context.Request.Url.LocalPath;
                if (IsFlioxRequest(path)) {
                    RequestContext response;
                    try {
                        // handle only request - does not write any response - except for accepting a websocket
                        response = await context.ExecuteFlioxRequest(httpHost).ConfigureAwait(false); // handle incoming requests parallel
                    }
                    catch (Exception e) {
                        var resp    = context.Response;
                        var message = $"fliox request failed - {e.GetType().Name}: {e.Message}";
                        LogException(message, e);
                        await HttpListenerExtensions.WriteResponseString(resp, "text/plain", 500, message, null).ConfigureAwait(false);
                        resp.Close();
                        return;
                    }
                    // write the response returned by ExecuteFlioxRequest()
                    await context.WriteFlioxResponse(response).ConfigureAwait(false);
                    return;
                }
                try {
                    await customRequestHandler(context).ConfigureAwait(false);
                }
                catch (Exception e) {
                    var message = $"custom request failed - {e.GetType().Name}: {e.Message}";
                    LogException(message, e);
                }
            });
        }
        
        public async Task ExecuteRequest(HttpListenerContext context) {
            var path        = context.Request.Url.LocalPath;
            var response    = context.Response;
            if (path == "/" && httpHost.endpoint != "/") {
                var location = httpHost.endpoint;
                var headers = new Dictionary<string, string> { { "Location", location }};
                await HttpListenerExtensions.WriteResponseString(response, "text/plain", 302, $"redirect -> {location}", headers).ConfigureAwait(false);
                response.OutputStream.Close(); // required by HttpListener in Unity for redirect. CLR does this automatically.
                return;
            }
            var body = $"{context.Request.Url} not found";
            await HttpListenerExtensions.WriteResponseString(response, "text/plain", 404, body, null).ConfigureAwait(false);
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
            var startPage = GetStartPage();
            sb.Append("Hub Explorer - ");
            sb.Append(startPage);
            sb.AppendLF();
            sb.AppendLF();
            sb.Append("Listening at:");
            foreach (var prefix in listener.Prefixes) {
                sb.Append(' ');
                sb.Append(prefix);
            }
            LogInfo(sb.ToString());
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

        private void LogException(string msg, Exception exception) {
            Logger.Log(HubLog.Error, msg, exception);
        }

        private void LogInfo(string msg) {
            Logger.Log(HubLog.Info, msg);
        }
        
        private string GetStartPage() {
            foreach (var prefix in listener.Prefixes) {
                if (prefix.Contains("+")) {
                    var url     = prefix.Replace("+", "localhost");
                    var trimEnd = url.EndsWith("/") ? 1 : 0;
                    return url.Substring(0, url.Length - trimEnd) + httpHost.endpoint;
                }
            }
            foreach (var prefix in listener.Prefixes) {
                return prefix;
            }
            return "http://localhost";
        }
    }
}
