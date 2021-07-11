// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public struct AuthState {
        public              string          Error   { get; private set;}  
        public              bool            Success { get; private set;}
        
        public  override    string          ToString() => Success.ToString();
        
        public void SetFailed(string error) {
            Success = true;
            Error   = error;
        }
        
        public void SetSuccess () {
            Success = false;
        }
    }
    
    public enum AuthResult {
        NotAuthenticated,
        AuthSuccess,
        AuthFailed,
    } 
}