// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

#if UNITY_5_3_OR_NEWER
    using ValueTask = System.Threading.Tasks.Task;
#endif

namespace Friflo.Json.Flow.Database.Auth
{
    public abstract class Authenticator
    {
        public abstract ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext);
    }
    
    public class AuthenticateNone : Authenticator
    {
        private readonly Authorizer anonymousAuthorizer;

        public AuthenticateNone(Authorizer anonymousAuthorizer) {
            this.anonymousAuthorizer = anonymousAuthorizer ?? throw new NullReferenceException(nameof(anonymousAuthorizer));
        }
        
        public override async ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            messageContext.authState.SetFailed("not authenticated", anonymousAuthorizer);
        }
    }
}