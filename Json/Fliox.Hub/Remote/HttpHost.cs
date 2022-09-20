// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// A <see cref="HttpHost"/> enables remote access to databases, schemas and static web files via
    /// <b>HTTP</b> or <b>WebSockets</b>.
    /// </summary>
    /// <remarks>
    /// The full feature set is listed at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#httphost">Host README.md</a><br/>
    /// In detail:
    /// <list type="bullet">
    ///   <item>hosted databases are given by the <see cref="FlioxHub"/> passed via its constructor
    ///     <see cref="HttpHost(FlioxHub, string, SharedEnv)"/>
    ///   </item>
    ///   <item>exposed schemas are retrieved from the hosted databases</item>
    ///   <item>static web files are exposed by adding a <see cref="StaticFileHandler"/> using <see cref="AddHandler"/></item>
    /// </list>
    /// 
    /// A <see cref="HttpHost"/> can be integrated by any HTTP server like like <b>ASP.NET Core / Kestrel</b>
    /// or the <see cref="System.Net.HttpListener"/> part of the .NET Base Class library (BCL). <br/>
    /// <br/>
    /// A <see cref="HttpHost"/> can be accessed remotely by: <br/>
    /// <list type="bullet">
    ///   <item>Send <b>Batch</b> requests using a <b>POST</b> to <b><c>./fliox</c></b> - containing multiple tasks </item>
    ///   <item>Send <b>Batch</b> requests containing multiple tasks via a <b>WebSocket</b> at <b><c>./fliox</c></b></item>
    ///   <item><b>REST</b> API to POST, GET, PUT, DELETE and PATCH with via a path like <b><c>./fliox/rest/database/container/id</c></b> </item>
    ///   <item><b>GraphQL</b> via an endpoint like <b><c>/fliox/graphql/database</c></b> - requires package: Friflo.Json.Fliox.Hub.GraphQL</item>
    /// </list>
    /// </remarks>
    public sealed class HttpHost : RemoteHost
    {
        /// <summary>never null, ends with '/'</summary>
        public   readonly   string                  endpoint; 
        private  readonly   string                  endpointRoot;
        private  readonly   SchemaHandler           schemaHandler   = new SchemaHandler();
        private  readonly   Rest.RestHandler        restHandler     = new Rest.RestHandler();
        private  readonly   List<IRequestHandler>   customHandlers  = new List<IRequestHandler>();
        private  readonly   List<string>            hubRoutes;
        
        public   const      string                  DefaultCacheControl = "max-age=600";

        public   override   string                  ToString() => $"endpoint: {endpoint}";

        private static  bool    _titleDisplayed;
        private const   string  JsonFlioxBanner =
@"    ____   _   _
   |  __| | | |_|  ____  __  __
   |  _|  | | | | | __ | \ \/ /
   |_|    |_| |_| |____| /_/\_\   ";
        
        private static void WriteBanner() {
            Console.Write(JsonFlioxBanner);
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(".oOo.  ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("..oo.  ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(".oOOo..");
            Console.WriteLine();
            Console.ForegroundColor = old;
        }

        public HttpHost(FlioxHub hub, string endpoint, SharedEnv env = null)
            : base(hub, env)
        {
            var msg = $"create HttpHost db: {hub.DatabaseName} ({hub.database.StorageType})";
            Logger.Log(HubLog.Info, msg);
            if (!_titleDisplayed) {
                _titleDisplayed = true;
                var hubName     = hub.Info.ToString();
                var hubLabel    = string.IsNullOrEmpty(hubName) ? "" : $"{hubName} - v{hub.HostVersion},   ";
                Logger.Log(HubLog.Info, $"{hubLabel}Friflo.Json.Fliox - v{FlioxHub.FlioxVersion}");
                WriteBanner();
            }
            hubRoutes = hub.routes;
            hubRoutes.AddRange(restHandler.Routes);
            hubRoutes.AddRange(schemaHandler.Routes);
            
            if (endpoint == null)           throw new ArgumentNullException(nameof(endpoint), "common values: \"/fliox/\" or \"/\"");
            if (!endpoint.StartsWith("/"))  throw new ArgumentException("endpoint requires '/' as first character");
            if (!endpoint.EndsWith("/"))    throw new ArgumentException("endpoint requires '/' as last character");
            this.endpoint           = endpoint;
            endpointRoot            = this.endpoint.Substring(0, this.endpoint.Length - 1);
            var protocolSchema      = NativeTypeSchema.Create(typeof(ProtocolMessage));
            var types               = ProtocolMessage.Types;
            var sepTypes            = protocolSchema.TypesAsTypeDefs(types);
            schemaHandler.AddSchema ("protocol", protocolSchema, sepTypes);
            //
            var filterSchema        = NativeTypeSchema.Create(typeof(FilterOperation));
            var filterRoot          = filterSchema.TypesAsTypeDefs(new [] {typeof(FilterOperation)});
            schemaHandler.AddSchema ("filter", filterSchema, filterRoot);
            //
            var jsonSchema          = NativeTypeSchema.Create(typeof(JSONSchema));
            var jsonSchemaRoot      = jsonSchema.TypesAsTypeDefs(new [] {typeof(JSONSchema)});
            schemaHandler.AddSchema ("json-schema", jsonSchema, jsonSchemaRoot);
        }
        
        public bool IsMatch (string path) {
            if (path.StartsWith(endpoint)) {
                return true;
            }
            return path == endpointRoot;
        }
        
        public bool GetRoute(string path, out string route) {
            if (path.StartsWith(endpoint)) {
                route = path.Substring(endpoint.Length - 1);
                return true;
            }
            route = null;
            return false;
        }
        
        /// <summary>
        /// Perform a redirect from /fliox -> /fliox/ <br/>
        /// Otherwise return a path mapping error. <br/>
        /// E.g. in case ASP.NET maps "/foo/{*path}" to a <see cref="HttpHost"/> using <see cref="endpoint"/> "/fliox/" 
        /// </summary>
        public RequestContext ExecuteUnknownPath(string path, string method) {
            if (path == endpointRoot && method == "GET") {
                var context = new RequestContext(this, "GET", path, null, null, null, null);
                context.AddHeader("Location", endpoint);
                context.WriteString($"redirect -> {endpoint}", "text/plain", 302);
                context.handled = true;
                return context;
            } else {
                var context = new RequestContext(this, method, path, null, null, null, null);
                var message = $"Expect path matching HttpHost.endpoint: {endpoint}, path: {path}";
                context.WriteError("internal path mapping error", message, 500);
                context.handled = true;
                return context;
            }
        }

        public string CacheControl {
            get => schemaHandler.CacheControl;
            set => schemaHandler.CacheControl = value;
        }
        
        public void AddHandler(IRequestHandler requestHandler) {
            if (requestHandler == null) throw new ArgumentNullException(nameof(requestHandler));
            customHandlers.Add(requestHandler);
            hubRoutes.AddRange(requestHandler.Routes);
        }
        
        public void RemoveHandler(IRequestHandler requestHandler) {
            customHandlers.Remove(requestHandler);
            foreach (var route in requestHandler.Routes) {
                hubRoutes.Remove(route);
            }
        }
        
        public void AddSchemaGenerator(string type, string name, SchemaGenerator generator) {
            schemaHandler.AddGenerator(type, name, generator);
        }
        
        /// <summary>
        /// Central point where all Fliox related HTTP requests arrive.
        /// Each request is dispatched by a matching request handler. 
        /// <br/>
        /// Note:
        /// Request matching and execution are separated to ensure no heap allocation caused by awaited method calls. 
        /// </summary>
        public async Task ExecuteHttpRequest(RequestContext request) {
            if (request.method == "POST" && request.route == "/") {
                var requestContent  = await JsonValue.ReadToEndAsync(request.body).ConfigureAwait(false);

                // Each request require its own pool as multiple request running concurrently. Could cache a Pool instance per connection.
                var pool        = sharedEnv.Pool;
                var syncContext = new SyncContext(pool, null, sharedEnv.sharedCache);
                var result      = await ExecuteJsonRequest(requestContent, syncContext).ConfigureAwait(false);
                
                syncContext.Release();
                request.Write(result.body, 0, "application/json", (int)result.status);
                request.handled = true;
                return;
            }
            if (schemaHandler.IsMatch(request)) {
                await schemaHandler.HandleRequest(request).ConfigureAwait(false);
                request.handled = true;
                return;
            }
            if (restHandler.IsMatch(request)) {
                await restHandler.HandleRequest(request).ConfigureAwait(false);
                request.handled = true;
                return;
            }
            foreach (var handler in customHandlers) {
                if (!handler.IsMatch(request))
                    continue;
                await handler.HandleRequest(request).ConfigureAwait(false);
                request.handled = true;
                return;
            }
        }
    }
}