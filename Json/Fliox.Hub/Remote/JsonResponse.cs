// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
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
        
        public static JsonResponse CreateError(MessageContext messageContext, string message, ErrorResponseType type)
        {
            var status          = type == ErrorResponseType.Exception ? JsonResponseStatus.Exception : JsonResponseStatus.Error;
            var errorResponse   = new ErrorResponse { message = message, type = type };
            using (var pooled = messageContext.pool.ObjectMapper.Get()) {
                ObjectWriter writer     = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var bodyArray           = writer.WriteAsArray<ProtocolMessage>(errorResponse);
                var body                = new JsonValue(bodyArray);
                return new JsonResponse(body, status);
            }
        }
    }
}