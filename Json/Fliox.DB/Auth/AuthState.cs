// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Host;

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
        /// <summary><see cref="User"/> is never null after calling <see cref="SetFailed"/> or <see cref="SetSuccess"/></summary>
        public              AuthUser    User            { get; private set;}
        private             bool        authExecuted;
        
        public  override    string      ToString() => authExecuted ? Authenticated ? "success" : "failed" : "pending";
        
        public void SetFailed(AuthUser user, string error, Authorizer authorizer) {
            User            = user ?? throw new ArgumentNullException(nameof(user));
            authExecuted    = true;
            Authenticated   = false;
            Authorizer      = authorizer;
            Error           = error;
        }
        
        public void SetSuccess (AuthUser user, Authorizer authorizer) {
            User            = user ?? throw new ArgumentNullException(nameof(user));
            authExecuted    = true;
            Authenticated   = true;
            Authorizer      = authorizer;
        }
    }
}