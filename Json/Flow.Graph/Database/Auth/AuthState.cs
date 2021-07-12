// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Database.Auth
{
    public struct AuthState {
        public              string                  Error           { get; private set;}  
        public              bool                    Authenticated   { get; private set;}
        public              ICollection<Authorizer> Authorizers     { get; private set;}
        private             bool                    authExecuted;
        
        public  override    string      ToString() => authExecuted ? Authenticated ? "success" : "failed" : "pending";
        
        public void SetFailed(string error, Authorizer authorizer) {
            authExecuted        = true;
            Authenticated       = false;
            Authorizers         = new List<Authorizer> { authorizer }; // performance: could use a SingleItemList<>
            Error               = error;
        }
        
        public void SetSuccess (ICollection<Authorizer> authorizers) {
            if (authorizers.Count == 0)
                throw new InvalidOperationException("Expect at least one element in authorizers");
            authExecuted        = true;
            Authenticated       = true;
            this.Authorizers    = authorizers;
        }
    }
}