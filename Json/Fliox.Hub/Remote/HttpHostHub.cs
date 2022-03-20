// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// A <see cref="HttpHostHub"/> enables remote access to databases, schemas and static web files via
    /// <b>HTTP</b> or <b>WebSockets</b><br/>
    /// In detail:
    /// <list type="bullet">
    ///   <item>hosted databases are given by the <see cref="FlioxHub"/> passed via its constructor
    ///     <see cref="HttpHostHub(FlioxHub, SharedEnv, string)"/>
    ///   </item>
    ///   <item>exposed schemas are retrieved from the hosted databases</item>
    ///   <item>static web files are exposed by adding a <see cref="StaticFileHandler"/> using <see cref="AddHandler"/></item>
    /// </list>
    /// 
    /// A <see cref="HttpHostHub"/> can be integrated by any HTTP server like like <b>ASP.NET Core / Kestrel</b>
    /// or the <see cref="System.Net.HttpListener"/> part of the .NET Base Class library (BCL). <br/>
    /// <br/>
    /// A <see cref="HttpHostHub"/> can be accessed remotely by: <br/>
    /// <list type="bullet">
    ///   <item>HTTP POST via a single path ./ enabling batching multiple tasks in a single request </item>
    ///   <item>Send batch requests containing multiple tasks via a WebSocket </item>
    ///   <item>Common REST API to POST, GET, PUT, DELETE and PATCH with via a path like ./rest/database/container/id </item>
    /// </list>
    /// </summary>
    public class HttpHostHub : RemoteHostHub
    {
        private  readonly   SchemaHandler           schemaHandler   = new SchemaHandler();
        private  readonly   RestHandler             restHandler     = new RestHandler();
        private  readonly   List<IRequestHandler>   customHandlers  = new List<IRequestHandler>();
        
        public   const      string                  DefaultCacheControl = "max-age=600";

        public HttpHostHub(FlioxHub hub, SharedEnv env = null, string hostName = null)
            : base(hub, env, hostName)
        {
            var protocolSchema      = new NativeTypeSchema(typeof(ProtocolMessage));
            var types               = ProtocolMessage.Types;
            var sepTypes            = protocolSchema.TypesAsTypeDefs(types);
            schemaHandler.AddSchema ("protocol", protocolSchema, sepTypes);
            //
            var filterSchema        = new NativeTypeSchema(typeof(FilterOperation));
            var filterRoot          = filterSchema.TypesAsTypeDefs(new [] {typeof(FilterOperation)});
            schemaHandler.AddSchema ("filter", filterSchema, filterRoot);
            //
            var jsonSchema          = new NativeTypeSchema(typeof(JSONSchema));
            var jsonSchemaRoot      = jsonSchema.TypesAsTypeDefs(new [] {typeof(JSONSchema)});
            schemaHandler.AddSchema ("json-schema", jsonSchema, jsonSchemaRoot);
        }
        
        public HttpHostHub CacheControl(string cacheControl) {
            schemaHandler.CacheControl(cacheControl);
            return this;
        }
        
        public void AddHandler(IRequestHandler requestHandler) {
            if (requestHandler == null) throw new ArgumentNullException(nameof(requestHandler));
            customHandlers.Add(requestHandler);
        }
        
        public void RemoveHandler(IRequestHandler requestHandler) {
            customHandlers.Remove(requestHandler);
        }
        
        public void AddSchemaGenerator(string type, string name, SchemaGenerator generator) {
            schemaHandler.AddGenerator(type, name, generator);
        }
        
        /// <summary>
        /// Central point where all Fliox related HTTP requests arrive.
        /// Each request is dispatched by a matching request handler. 
        /// <br/>
        /// Note:
        /// Request matching and execution are seperated to ensure no heap allocation caused by awaited method calls. 
        /// </summary>
        public async Task ExecuteHttpRequest(RequestContext request) {
            if (request.method == "POST" && request.path == "/") {
                var requestContent  = await JsonValue.ReadToEndAsync(request.body).ConfigureAwait(false);

                // Each request require its own pool as multiple request running concurrently. Could cache a Pool instance per connection.
                var pool            = new Pool(sharedEnv.Pool);
                var executeContext  = new ExecuteContext(pool, null, sharedEnv.sharedCache);
                var result          = await ExecuteJsonRequest(requestContent, executeContext).ConfigureAwait(false);
                
                executeContext.Release();
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