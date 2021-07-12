// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public struct AuthState {
        public      string      Error           { get; private set;}  
        public      bool        Authenticated   { get; private set;}
        private     Authorizer  authorizer;
        private     bool        authExecuted;
        
        public  override    string          ToString() => authExecuted ? (Authenticated ? "success" : "failed") : "pending";
        
        public bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return authorizer.Authorize(task, messageContext);
        }
        
        public void SetFailed(string error, Authorizer authorizer) {
            authExecuted    = true;
            Authenticated   = false;
            this.authorizer = authorizer;
            Error           = error;
        }
        
        public void SetSuccess (Authorizer authorizer) {
            authExecuted    = true;
            Authenticated   = true;
            this.authorizer = authorizer;
        }
    }
}