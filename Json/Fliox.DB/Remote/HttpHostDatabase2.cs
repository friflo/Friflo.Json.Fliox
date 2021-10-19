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
    public class HttpHostDatabase : RemoteHostDatabase
    {
        public              SchemaHub           schemaHub;
        public              IRequestHandler     requestHandler;
        
        private  readonly   SchemaHub           protocolSchemaHub;

        
        public HttpHostDatabase(EntityDatabase local, DbOpt opt = null) : base(local, opt) {
            var protocolSchema      = new NativeTypeSchema(typeof(ProtocolMessage));
            var types               = ProtocolMessage.Types;
            var sepTypes            = protocolSchema.TypesAsTypeDefs(types);
            protocolSchemaHub       = new SchemaHub("/protocol/", protocolSchema, sepTypes);
        }
        
        public async Task<JsonResponse> ExecuteHttpRequest(RequestContext reqCtx) {
            var requestContent  = await JsonUtf8.ReadToEndAsync(reqCtx.body).ConfigureAwait(false);
            var pools           = new Pools(UtilsInternal.SharedPools);
            var messageContext  = new MessageContext(pools, null);
            return await ExecuteJsonRequest(requestContent, messageContext);
        }

        protected async Task<bool> HandleRequest(RequestContext request) {
            if (schemaHub != null) {
                if (await schemaHub.HandleRequest(request).ConfigureAwait(false))
                    return true;
            }
            if (await protocolSchemaHub.HandleRequest(request).ConfigureAwait(false))
                return true;
            if (requestHandler != null) { 
                if (await requestHandler.HandleRequest(request).ConfigureAwait(false))
                    return true;
            }
            return false;
        }
    }
}