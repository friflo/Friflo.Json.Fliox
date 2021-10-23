// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.DB.Remote
{
    public class HttpHostHub : RemoteHostHub
    {
        public              SchemaHandler       schemaHandler;
        public              IRequestHandler     requestHandler;
        
        private  readonly   SchemaHandler       protocolSchemaHandler;

        
        public HttpHostHub(FlioxHub hub, string hostName = null) : base(hub, hostName) {
            var protocolSchema      = new NativeTypeSchema(typeof(ProtocolMessage));
            var types               = ProtocolMessage.Types;
            var sepTypes            = protocolSchema.TypesAsTypeDefs(types);
            protocolSchemaHandler   = new SchemaHandler("/protocol/", protocolSchema, sepTypes);
        }
        
        public async Task<bool> ExecuteHttpRequest(RequestContext reqCtx) {
            if (reqCtx.method == "POST" && reqCtx.path == "/") {
                var requestContent  = await JsonValue.ReadToEndAsync(reqCtx.body).ConfigureAwait(false);

                // Each request require its own pool as multiple request running concurrently. Could cache a Pools instance per connection.
                var pools           = new Pools(UtilsInternal.SharedPools);
                var messageContext  = new MessageContext(pools, null);
                var result          = await ExecuteJsonRequest(requestContent, messageContext).ConfigureAwait(false);
                
                messageContext.Release();
                reqCtx.Write(result.body, 0, "application/json", (int)result.status);
                return true;
            }
            return await HandleRequest(reqCtx).ConfigureAwait(false);
        }

        private async Task<bool> HandleRequest(RequestContext request) {
            if (schemaHandler != null) {
                if (await schemaHandler.HandleRequest(request).ConfigureAwait(false))
                    return true;
            }
            if (await protocolSchemaHandler.HandleRequest(request).ConfigureAwait(false))
                return true;
            if (requestHandler == null)
                return false;
            return await requestHandler.HandleRequest(request).ConfigureAwait(false);
        }
    }
}