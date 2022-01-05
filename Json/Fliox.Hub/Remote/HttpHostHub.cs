// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public class HttpHostHub : RemoteHostHub
    {
        public              SchemaHandler       schemaHandler;
        public              IRequestHandler     requestHandler;
        
        private  readonly   SchemaHandler       protocolSchemaHandler;
        private  readonly   RestHandler         restHandler;

        
        public HttpHostHub(FlioxHub hub, SharedEnv env = null, string hostName = null)
            : base(hub, env, hostName)
        {
            var protocolSchema      = new NativeTypeSchema(typeof(ProtocolMessage));
            var types               = ProtocolMessage.Types;
            var sepTypes            = protocolSchema.TypesAsTypeDefs(types);
            protocolSchemaHandler   = new SchemaHandler(protocolSchema, sepTypes);
            restHandler             = new RestHandler(hub);
        }
        
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
            if (schemaHandler != null && schemaHandler.IsApplicable(request)) {
                await schemaHandler.HandleRequest(request).ConfigureAwait(false);
                return true;
            }
            if (restHandler.IsApplicable(request)) {
                await restHandler.HandleRequest(request).ConfigureAwait(false);
                return true;
            }
            if (protocolSchemaHandler.IsApplicable(request)) {
                await protocolSchemaHandler.HandleRequest(request).ConfigureAwait(false);
                return true;
            }
            if (requestHandler != null && requestHandler.IsApplicable(request)) {
                await requestHandler.HandleRequest(request).ConfigureAwait(false);
                return true;
            }
            return false;
        }
    }
}