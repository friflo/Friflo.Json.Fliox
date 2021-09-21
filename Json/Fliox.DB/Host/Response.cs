// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    public class Response<TResponse> where TResponse : ProtocolResponse {
        public  readonly    TResponse       sucess;
        public  readonly    ErrorResponse   error;

        public Response (TResponse response) {
            this.sucess = response;
        }
        
        public Response (string errorMessage) {
            error = new ErrorResponse { message = errorMessage };
        }
        
        public  ProtocolResponse Result { get {
            if (sucess != null)
                return sucess;
            return error;
        } }

        public override string ToString() => sucess != null ? sucess.ToString() : error.ToString();
    }
}