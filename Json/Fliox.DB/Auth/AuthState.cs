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
        public              User        User            { get; private set;}
        internal            bool        AuthExecuted    { get; private set;}
        
        public  override    string      ToString() => AuthExecuted ? Authenticated ? "success" : "failed" : "pending";
        
        public void SetFailed(User user, string error, Authorizer authorizer) {
            if (AuthExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            User            = user ?? throw new ArgumentNullException(nameof(user));
            AuthExecuted    = true;
            Authenticated   = false;
            Authorizer      = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
            Error           = error;
        }
        
        public void SetSuccess (User user, Authorizer authorizer) {
            if (AuthExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            User            = user ?? throw new ArgumentNullException(nameof(user));
            AuthExecuted    = true;
            Authenticated   = true;
            Authorizer      = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
        }
    }
}