// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Database.Auth
{
    public struct AuthState {
        public              string      Error           { get; private set;}  
        public              bool        Authenticated   { get; private set;}
        public              Authorizer  Authorizer      { get; private set;}
        private             bool        authExecuted;
        
        public  override    string      ToString() => authExecuted ? Authenticated ? "success" : "failed" : "pending";
        
        public void SetFailed(string error, Authorizer authorizer) {
            authExecuted    = true;
            Authenticated   = false;
            Authorizer      = authorizer;
            Error           = error;
        }
        
        public void SetSuccess (Authorizer authorizer) {
            authExecuted    = true;
            Authenticated   = true;
            Authorizer      = authorizer;
        }
    }
}