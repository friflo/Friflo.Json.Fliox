// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.Native;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Remote
{
    public class HttpHostHub : RemoteHostHub
    {
        private  readonly   SchemaHandler           schemaHandler;
        private  readonly   RestHandler             restHandler;
        private  readonly   List<IRequestHandler>   customHandlers;

        public HttpHostHub(FlioxHub hub, SharedEnv env = null, string hostName = null)
            : base(hub, env, hostName)
        {
            var protocolSchema      = new NativeTypeSchema(typeof(ProtocolMessage));
            var types               = ProtocolMessage.Types;
            var sepTypes            = protocolSchema.TypesAsTypeDefs(types);
            schemaHandler           = new SchemaHandler(hub, ZipUtils.Zip);
            schemaHandler.AddSchema ("protocol", protocolSchema, sepTypes);
            restHandler             = new RestHandler(hub);
            customHandlers          = new List<IRequestHandler>();
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
                var messageContext  = new MessageContext(pool, null);
                var result          = await ExecuteJsonRequest(requestContent, messageContext).ConfigureAwait(false);
                
                messageContext.Release();
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