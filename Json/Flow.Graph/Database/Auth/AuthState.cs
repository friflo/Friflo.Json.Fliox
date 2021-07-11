// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public struct AuthState {
        public  ClientAuthenticator authenticator;
        public  SyncRequest         syncRequest;
        public  string              error;
        public  AuthResult          result;
        
        public void SetFailed(string error) {
            result      = AuthResult.AuthFailed;
            this.error  = error;
        }
        
        public void SetSuccess () {
            this.result = AuthResult.AuthSuccess;
        }
    }
    
    public enum AuthResult {
        NotAuthenticated,
        AuthSuccess,
        AuthFailed,
    } 
}