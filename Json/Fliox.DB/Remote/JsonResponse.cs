// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    public enum JsonResponseStatus {
        /// maps to HTTP 200 OK
        Ok          = 200,         
        /// maps to HTTP 400 Bad Request
        Error       = 400,
        /// maps to HTTP 500 Internal Server Error
        Exception   = 500
    }
    
    public sealed class JsonResponse
    {
        public readonly     JsonValue           body;
        public readonly     JsonResponseStatus  status;
        
        public JsonResponse(JsonValue body, JsonResponseStatus status) {
            this.body   = body;
            this.status = status;
        }
        
        public static JsonResponse CreateError(MessageContext messageContext, string message, JsonResponseStatus type)
        {
            var errorResponse = new ErrorResponse {message = message};
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                ObjectWriter writer     = pooledMapper.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var bodyArray           = writer.WriteAsArray<ProtocolMessage>(errorResponse);
                var body                = new JsonValue(bodyArray);
                return new JsonResponse(body, type);
            }
        }
    }
}