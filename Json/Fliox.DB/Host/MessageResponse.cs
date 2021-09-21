// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    public class MessageResponse<TResponse> where TResponse : ProtocolResponse {
        public  TResponse       result;
        public  ErrorResponse   error;

        public MessageResponse (TResponse response) {
            this.result = response;
        }
        
        public MessageResponse (string errorMessage) {
            error = new ErrorResponse { message = errorMessage };
        }
        
        public  ProtocolResponse Response { get {
            if (result != null)
                return result;
            return error;
        } }

        public override string ToString() => result != null ? result.ToString() : error.ToString();
    }
}