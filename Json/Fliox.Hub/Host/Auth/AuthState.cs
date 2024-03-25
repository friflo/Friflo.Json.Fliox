// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Contains the authentication and authorization result of <see cref="Authenticator.AuthenticateAsync"/>.
    /// The authentication is performed for every <see cref="FlioxHub.ExecuteRequestAsync"/> call. 
    /// </summary>
    internal struct AuthState {
        internal            string          error;  
        internal            bool            authenticated;
        internal            TaskAuthorizer  taskAuthorizer;     // not null
        internal            HubPermission   hubPermission;      // not null
        /// <summary><see cref="user"/> is never null after calling <see cref="SyncContext.AuthenticationFailed"/>
        /// or <see cref="SyncContext.AuthenticationSucceed"/></summary>
        internal            User            user;               // not null
        internal            bool            authExecuted;
        
        public  override    string          ToString() => authExecuted ? authenticated ? "success" : "failed" : "pending";
    }
}