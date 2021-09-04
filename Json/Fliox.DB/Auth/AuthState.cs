// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.NoSQL;

namespace Friflo.Json.Fliox.DB.Auth
{
    /// <summary>
    /// Contains the authentication and authorization result of <see cref="Authenticator.Authenticate"/>.
    /// The authentication is performed for every <see cref="EntityDatabase.ExecuteSync"/> call. 
    /// </summary>
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