// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    public readonly struct MsgResponse<TResponse> where TResponse : ProtocolResponse {
        public  readonly    TResponse       success;
        public  readonly    ErrorResponse   error;

        public MsgResponse (TResponse successResponse) {
            success = successResponse;
            error   = null;
        }
        
        public MsgResponse (string errorMessage) {
            success = null;
            error   = new ErrorResponse { message = errorMessage };
        }
        
        public  ProtocolResponse Result { get {
            if (success != null)
                return success;
            return error;
        } }

        public override string ToString() => success != null ? success.ToString() : error.ToString();
    }
}