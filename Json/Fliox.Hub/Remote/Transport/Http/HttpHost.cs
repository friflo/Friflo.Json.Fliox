// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote.Rest;
using Friflo.Json.Fliox.Hub.Remote.Schema;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Transform;
using static Friflo.Json.Fliox.Hub.Remote.Rest.RestRequestType;
using static Friflo.Json.Fliox.Hub.Remote.Rest.RestUtils;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable once CheckNamespace
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
    public sealed class HttpHost : IHttpHost, ILogSource, IDisposable
    {
                        /// <summary>never null, ends with '/'</summary>
                        public   readonly   string                  baseRoute; 
                        public   readonly   FlioxHub                hub;
                        public   readonly   SharedEnv               sharedEnv;
                        public              List<string>            Routes      => routes.ToList();
        [Browse(Never)] public              IHubLogger              Logger      => sharedEnv.hubLogger;
                        public              AcceptWebSocketType     AcceptWebSocketType   { get; init; } = AcceptWebSocketType.SystemNet;
        
                        public   const      string                  DefaultCacheControl = "max-age=600";
        
        // --- private / internal
        [Browse(Never)] internal readonly   string                  baseRouteRoot;
                        private  readonly   SchemaHandler           schemaHandler   = new SchemaHandler();
                        private  readonly   RestHandler             restHandler     = new RestHandler();
                        private  readonly   List<IRequestHandler>   customHandlers  = new List<IRequestHandler>();
        [Browse(Never)] private  readonly   SortedSet<string>       routes          = new SortedSet<string>();

                        public   override   string                  ToString() => $"{baseRoute}  hostName: {hub.HostName}";

        private  static     bool    _titleDisplayed;
        private  const      string  JsonFlioxBanner =
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

        public HttpHost(FlioxHub hub, string baseRoute, SharedEnv env = null)
        {
            sharedEnv   = env  ?? SharedEnv.Default;
            this.hub    = hub;
            var msg = $"create HttpHost db: '{hub.database.name}' ({hub.database.StorageType})";
            Logger.Log(HubLog.Info, msg);
            if (!_titleDisplayed) {
                _titleDisplayed = true;
                var hubName     = hub.Info.ToString();
                var hubLabel    = string.IsNullOrEmpty(hubName) ? "" : $"{hubName} - v{hub.HostVersion},   ";
                Logger.Log(HubLog.Info, $"{hubLabel}Friflo.Json.Fliox - v{FlioxHub.FlioxVersion}");
                WriteBanner();
            }
            routes.UnionWith(restHandler.Routes);
            routes.UnionWith(schemaHandler.Routes);
            
            if (baseRoute == null)           throw new ArgumentNullException(nameof(baseRoute), "common values: \"/fliox/\" or \"/\"");
            if (!baseRoute.StartsWith("/"))  throw new ArgumentException("endpoint requires '/' as first character");
            if (!baseRoute.EndsWith("/"))    throw new ArgumentException("endpoint requires '/' as last character");
            this.baseRoute          = baseRoute;
            baseRouteRoot            = baseRoute.Substring(0, baseRoute.Length - 1);
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

        public void Dispose() { }

        public string CacheControl {
            get => schemaHandler.CacheControl;
            set => schemaHandler.CacheControl = value;
        }
        
        public void AddHandler(IRequestHandler requestHandler) {
            if (requestHandler == null) throw new ArgumentNullException(nameof(requestHandler));
            customHandlers.Add(requestHandler);
            routes.UnionWith(requestHandler.Routes);
        }
        
        public void RemoveHandler(IRequestHandler requestHandler) {
            customHandlers.Remove(requestHandler);
            foreach (var route in requestHandler.Routes) {
                routes.Remove(route);
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
        public async Task ExecuteHttpRequest(RequestContext cx) {
            if (cx.method == "POST" && cx.route == "/") {
                var requestContent  = await JsonValue.ReadToEndAsync(cx.body, cx.contentLength).ConfigureAwait(false);

                // Each request require its own pool as multiple request running concurrently. Could cache a Pool instance per connection.
                var pool        = sharedEnv.pool;
                var syncContext = new SyncContext(sharedEnv, null, cx.memoryBuffer) { Host = cx.host }; // new context per request
                using (var pooledMapper = pool.ObjectMapper.Get()) {
                    var mapper  = pooledMapper.instance;
                    var writer  = MessageUtils.GetPrettyWriter(mapper);
                    // inlined ExecuteJsonRequest() to avoid async call:
                    // JsonResponse response  = await ExecuteJsonRequest(mapper, requestContent, syncContext).ConfigureAwait(false);
                    JsonResponse response;
                    try {
                        var syncRequest = MessageUtils.ReadSyncRequest(mapper.reader, sharedEnv, requestContent, out string error);
                        if (error != null) {
                            response = JsonResponse.CreateError(writer, error, ErrorResponseType.BadResponse, null);
                        } else {
                            var executionType   = hub.InitSyncRequest(syncRequest);
                            ExecuteSyncResult syncResult;
                            switch (executionType) {
                                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
                            }
                            response = RemoteHostUtils.CreateJsonResponse(syncResult, syncRequest.reqId, hub.sharedEnv, writer);
                        }
                    }
                    catch (Exception e) {
                        var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                        response = JsonResponse.CreateError(writer, errorMsg, ErrorResponseType.Exception, null);
                    }
                    var body        = new JsonValue(response.body); // create copy => result.body array may change when the pooledMapper is reused
                    cx.Write(body, "application/json", (int)response.status);
                    cx.handled = true;
                    return;
                }
            }
            if (schemaHandler.IsMatch(cx)) {
                await schemaHandler.HandleRequest(cx).ConfigureAwait(false);
                cx.handled = true;
                return;
            }
            if (RestHandler.IsMatch(cx)) {
                JsonValue body = default; 
                if (cx.method == "POST" || cx.method == "PUT" || cx.method == "PATCH") {
                    body = await JsonValue.ReadToEndAsync(cx.body, cx.contentLength).ConfigureAwait(false);
                }
                var rr = RestHandler.GetRestRequest(cx, body); // rr looks russian :D
                // execute REST request from here instead extracting to a method to avoid additional async call
                switch (rr.type) {
                    // --- error
                    case error:      cx.WriteError      (rr.errorType, rr.errorMessage, rr.errorStatus);    break;
                    // --- message / command
                    case command: await Command         (cx, rr.db, rr.message, rr.value)                   .ConfigureAwait(false); break;
                    case message: await Message         (cx, rr.db, rr.message, rr.value)                   .ConfigureAwait(false); break;
                    // --- container operations
                    case read:    await GetEntitiesById (cx, rr.db, rr.container, rr.keys)                  .ConfigureAwait(false); break;
                    case readOne: await GetEntity       (cx, rr.db, rr.container, rr.id)                    .ConfigureAwait(false); break;
                    case query:   await QueryEntities   (cx, rr.db, rr.container, rr.query)                 .ConfigureAwait(false); break;
                    case write:   await PutEntities     (cx, rr.db, rr.container, rr.id, rr.value, rr.query).ConfigureAwait(false); break;
                    case merge:   await MergeEntities   (cx, rr.db, rr.container, rr.id, rr.value, rr.query).ConfigureAwait(false); break;
                    case delete:  await DeleteEntities  (cx, rr.db, rr.container, rr.keys)                  .ConfigureAwait(false); break;
                }
                cx.handled = true; 
                return;
            }
            foreach (var handler in customHandlers) {
                if (!handler.IsMatch(cx))
                    continue;
                if (await handler.HandleRequest(cx).ConfigureAwait(false)) {
                    cx.handled = true;
                    return;
                }
            }
            cx.WriteError("file not found", $"path: {cx.route}", 404);
            cx.handled = true;
        }
        
        public static string GetArg(string[] args, string name) {
            for (int n = 0; n < args.Length; n++) {
                var arg = args[n];
                if (arg != name) {
                    continue;
                }
                if (++n < args.Length) {
                    return args[n]; 
                }
                break;
            }
            return null;
        }
    }
}