// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// A <see cref="HttpHostHub"/> enables remote access to <see cref="EntityDatabase"/> instances hosted
    /// by a <see cref="FlioxHub"/> <br/>
    /// The <see cref="HttpHostHub"/> provide a set of Web APIs to access its databases from remote clients
    /// by using <b>HTTP</b> or <b>WebSockets</b>. <br/>
    /// A <see cref="HttpHostHub"/> can be integrated by any HTTP server like like <b>ASP.NET Core / Kestrel</b>
    /// or the <see cref="System.Net.HttpListener"/> part of the .NET Base Class library (BCL). <br/>
    /// <br/>
    /// A <see cref="HttpHostHub"/> can be accessed remotely by: <br/>
    ///   1. HTTP POST via a single path ./ enabling batching multiple tasks in a single request <br/>
    ///   2. Send batch requests containing multiple tasks via a WebSocket <br/>
    ///   3. Common REST API to POST, GET, PUT, DELETE and PATCH with via a path like ./rest/database/container/id <br/>
    /// </summary>
    public class HttpHostHub : RemoteHostHub
    {
        private  readonly   SchemaHandler           schemaHandler;
        private  readonly   RestHandler             restHandler;
        private  readonly   List<IRequestHandler>   customHandlers;
        
        public   const      string                  DefaultCacheControl = "max-age=600";

        public HttpHostHub(FlioxHub hub, SharedEnv env = null, string hostName = null)
            : base(hub, env, hostName)
        {
            var protocolSchema      = new NativeTypeSchema(typeof(ProtocolMessage));
            var types               = ProtocolMessage.Types;
            var sepTypes            = protocolSchema.TypesAsTypeDefs(types);
            schemaHandler           = new SchemaHandler(hub, ZipUtils.Zip);
            schemaHandler.AddSchema ("protocol", protocolSchema, sepTypes);
            var filterSchema        = new NativeTypeSchema(typeof(FilterOperation));
            var filterRoot          = filterSchema.TypesAsTypeDefs(new [] {typeof(FilterOperation)});
            schemaHandler.AddSchema ("filter", filterSchema, filterRoot);
            restHandler             = new RestHandler(hub);
            customHandlers          = new List<IRequestHandler>();
        }
        
        public HttpHostHub CacheControl(string cacheControl) {
            schemaHandler.CacheControl(cacheControl);
            return this;
        }
        
        public void AddHandler(IRequestHandler requestHandler) {
            customHandlers.Add(requestHandler);
        }
        
        public void RemoveHandler(IRequestHandler requestHandler) {
            customHandlers.Remove(requestHandler);
        }
        
        public void AddSchemaGenerator(string type, string name, Func<GeneratorOptions, SchemaSet> generate) {
            schemaHandler.AddGenerator(type, name, generate);
        }
        
        /// <summary>
        /// Central point where all Fliox related HTTP requests arrive.
        /// Each request is dispatched by a matching request handler. 
        /// <br/>
        /// Note:
        /// Request matching and execution are seperated to ensure no heap allocation caused by awaited method calls. 
        /// </summary>
        public async Task<bool> ExecuteHttpRequest(RequestContext request) {
            if (request.method == "POST" && request.path == "/") {
                var requestContent  = await JsonValue.ReadToEndAsync(request.body).ConfigureAwait(false);

                // Each request require its own pool as multiple request running concurrently. Could cache a Pool instance per connection.
                var pool            = new Pool(sharedEnv.Pool);
                var executeContext  = new ExecuteContext(pool, null, sharedEnv.sharedCache);
                var result          = await ExecuteJsonRequest(requestContent, executeContext).ConfigureAwait(false);
                
                executeContext.Release();
                request.Write(result.body, 0, "application/json", (int)result.status);
                return true;
            }
            if (schemaHandler != null && schemaHandler.IsMatch(request)) {
                await schemaHandler.HandleRequest(request).ConfigureAwait(false);
                return true;
            }
            if (restHandler.IsMatch(request)) {
                await restHandler.HandleRequest(request).ConfigureAwait(false);
                return true;
            }
            foreach (var handler in customHandlers) {
                if (!handler.IsMatch(request))
                    continue;
                await handler.HandleRequest(request).ConfigureAwait(false);
                return true;
            }
            return false;
        }
    }
}