// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public struct AuthState {
        public      string              Error           { get; private set;}  
        public      bool                Authenticated   { get; private set;}
        private     List<Authorizer>    authorizers;
        private     bool                authExecuted;
        
        public  override    string      ToString() => authExecuted ? Authenticated ? "success" : "failed" : "pending";
        
        public bool Authorize(DatabaseTask task, MessageContext messageContext) {
            foreach (var authorizer in authorizers) {
                if (!authorizer.Authorize(task, messageContext))
                    return false;
            }
            return true;
        }
        
        public void SetFailed(string error, Authorizer authorizer) {
            authExecuted        = true;
            Authenticated       = false;
            authorizers         = new List<Authorizer> { authorizer };
            Error               = error;
        }
        
        public void SetSuccess (ICollection<Authorizer> authorizers) {
            if (authorizers.Count == 0)
                throw new InvalidOperationException("Expect at least one element in authorizers");
            authExecuted        = true;
            Authenticated       = true;
            this.authorizers    = authorizers.ToList();
        }
    }
}