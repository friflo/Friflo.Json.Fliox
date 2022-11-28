// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal enum JsonResponseStatus {
        /// maps to HTTP 200 OK
        Ok          = 200,         
        /// maps to HTTP 400 Bad Request
        Error       = 400,
        /// maps to HTTP 500 Internal Server Error
        Exception   = 500
    }
    
    internal readonly struct JsonResponse
    {
        public readonly     JsonValue           body;
        public readonly     JsonResponseStatus  status;
        
        public JsonResponse(in JsonValue body, JsonResponseStatus status) {
            this.body   = body;
            this.status = status;
        }
        
        public static JsonResponse CreateError(ObjectPool<ObjectMapper> mapperPool, string message, ErrorResponseType type, int? reqId)
        {
            var status          = type == ErrorResponseType.Exception ? JsonResponseStatus.Exception : JsonResponseStatus.Error;
            var errorResponse   = new ErrorResponse { message = message, type = type, reqId = reqId };
            using (var pooled = mapperPool.Get()) {
                ObjectWriter writer     = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var body                = writer.WriteAsValue<ProtocolMessage>(errorResponse);
                return new JsonResponse(body, status);
            }
        }
    }
}