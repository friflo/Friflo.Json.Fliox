// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.Hub.Auth
{
    /// <summary>
    /// Contains the authentication and authorization result of <see cref="Authenticator.Authenticate"/>.
    /// The authentication is performed for every <see cref="FlioxHub.ExecuteSync"/> call. 
    /// </summary>
    internal struct AuthState {
        internal            string      error;  
        internal            bool        authenticated;
        internal            Authorizer  authorizer;
        /// <summary><see cref="user"/> is never null after calling <see cref="MessageContext.AuthenticationFailed"/>
        /// or <see cref="MessageContext.AuthenticationSucceed"/></summary>
        internal            User        user;
        internal            bool        authExecuted;
        
        public  override    string      ToString() => authExecuted ? authenticated ? "success" : "failed" : "pending";
    }
}