// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Flow.Database.Auth
{
    public struct AuthState {
        public      string  Error           { get; private set;}  
        public      bool    Authenticated   { get; private set;}
        private     bool    authExecuted;      
        
        public  override    string          ToString() => authExecuted ? (Authenticated ? "success" : "failed") : "pending";
        
        public void SetFailed(string error) {
            authExecuted    = true;
            Authenticated   = false;
            Error           = error;
        }
        
        public void SetSuccess () {
            authExecuted    = true;
            Authenticated   = true;
        }
    }
}