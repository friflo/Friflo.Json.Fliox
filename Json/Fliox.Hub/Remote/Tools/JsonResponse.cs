// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

namespace Friflo.Json.Fliox.Hub.Remote.Tools
{
    public enum JsonResponseStatus {
        /// maps to HTTP 200 OK
        Ok          = 200,         
        /// maps to HTTP 400 Bad Request
        Error       = 400,
        /// maps to HTTP 500 Internal Server Error
        Exception   = 500
    }
    
    public readonly struct JsonResponse
    {
        /// <summary><b>Attention</b> <see cref="body"/> is <b>only</b> valid until used <see cref="ObjectMapper"/> is reused </summary>
        public readonly     JsonValue           body;
        public readonly     JsonResponseStatus  status;
        
        public JsonResponse(in JsonValue body, JsonResponseStatus status) {
            this.body   = body;
            this.status = status;
        }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonResponse"/> is <b>only</b> valid until the passed <paramref name="writer"/> is reused
        /// </summary>
        public static JsonResponse CreateError(ObjectWriter writer, string message, ErrorResponseType type, int? reqId)
        {
            var status          = type == ErrorResponseType.Exception ? JsonResponseStatus.Exception : JsonResponseStatus.Error;
            var errorResponse   = new ErrorResponse { message = message, type = type, reqId = reqId };
            var body            = writer.WriteAsBytes<ProtocolMessage>(errorResponse);
            return new JsonResponse(new JsonValue(body), status);
        }
    }
}